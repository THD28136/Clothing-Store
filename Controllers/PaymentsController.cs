using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Helpers;
using MTKPM_Clothing_Store_web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;
using System.Linq;
using System.Globalization;
using System.Security.Claims;

namespace MTKPM_Clothing_Store_web.Controllers
{
    [AllowAnonymous]
    public class PaymentsController : Controller
    {
        // Single, unambiguous DbContext field (ApplicationDbContext is registered in DI)
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpAccessor;
        private readonly ILogger<PaymentsController> _loggerInstance;

        public PaymentsController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<PaymentsController> logger)
        {
            this._dbContext = context;
            this._httpAccessor = httpContextAccessor;
            this._loggerInstance = logger;
        }

        // GET: show checkout page
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = this._httpAccessor.HttpContext?.Session.GetObject<List<SessionCartItem>>("Cart") ?? new List<SessionCartItem>();
            if (!cart.Any()) return RedirectToAction("Index", "Cart");

            var vm = new CheckoutViewModel();
            foreach (var ci in cart)
            {
                var prod = await this._dbContext.Products.FindAsync(ci.ProductId);
                if (prod == null) continue;
                vm.Items.Add(new CheckoutItem { Product = prod, Quantity = ci.Quantity });
            }

            // populate UserId for convenience (if authenticated or session has UserId)
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? _httpAccessor.HttpContext?.Session.GetString("UserId");
            if (int.TryParse(userIdStr, out var uid))
            {
                vm.UserId = uid;
            }

            return View(vm);
        }

        // POST: mock payment (school project) with raw-insert Order creation workaround
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutPost(CheckoutViewModel model)
        {
            // repopulate items from session
            var cart = _httpAccessor.HttpContext?.Session.GetObject<List<SessionCartItem>>("Cart") ?? new List<SessionCartItem>();
            if (!cart.Any()) return RedirectToAction("Index", "Cart");

            model.Items.Clear();
            foreach (var ci in cart)
            {
                var prod = await _dbContext.Products.FindAsync(ci.ProductId);
                if (prod == null) continue;
                model.Items.Add(new CheckoutItem { Product = prod, Quantity = ci.Quantity });
            }

            // model validation
            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            // expiry date validation
            var now = DateTime.UtcNow;
            if (model.ExpiryYear < now.Year || (model.ExpiryYear == now.Year && model.ExpiryMonth < now.Month))
            {
                ModelState.AddModelError(string.Empty, "Ngày hết hạn thẻ đã qua. Vui lòng kiểm tra lại.");
                return View("Checkout", model);
            }

            // Basic card-number sanitization (we are not performing real payment)
            var normalizedCard = new string(model.CardNumber.Where(char.IsDigit).ToArray());
            if (normalizedCard.Length < 12 || normalizedCard.Length > 19)
            {
                ModelState.AddModelError(nameof(model.CardNumber), "Số thẻ không hợp lệ.");
                return View("Checkout", model);
            }

            // Ensure we have the current user id (prefer claim, fallback to session)
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? _httpAccessor.HttpContext?.Session.GetString("UserId");
            int? userId = null;
            if (int.TryParse(userIdStr, out var uid)) userId = uid;

            // Guest checkout: no account, so require an email instead of a login
            string? guestEmail = null;
            if (userId == null)
            {
                guestEmail = model.GuestEmail?.Trim();
                if (string.IsNullOrWhiteSpace(guestEmail))
                {
                    ModelState.AddModelError(nameof(model.GuestEmail), "Vui lòng nhập email để nhận xác nhận đơn hàng.");
                    return View("Checkout", model);
                }
            }
            model.UserId = userId;

            // CVV check already done by DataAnnotations; proceed with mock payment processing
            try
            {
                _loggerInstance.LogInformation("Mock checkout started for user {UserId}. Cart: {@Cart}", userId, cart);

                // Basic pre-check: ensure all products exist and have enough stock
                foreach (var ci in cart)
                {
                    var prod = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == ci.ProductId);
                    if (prod == null)
                    {
                        _loggerInstance.LogWarning("Mock checkout: product {ProductId} not found.", ci.ProductId);
                        return BadRequest("Một sản phẩm không tồn tại. Vui lòng kiểm tra giỏ hàng.");
                    }

                    if (prod.Stock < ci.Quantity)
                    {
                        _loggerInstance.LogInformation("Mock checkout: insufficient stock for product {ProductId} (need {Need}, have {Have}).", ci.ProductId, ci.Quantity, prod.Stock);
                        return BadRequest($"Không đủ hàng cho sản phẩm '{prod.Name}'. Có {prod.Stock} trong kho.");
                    }
                }

                // ----- WORKAROUND: create order via raw SQL to avoid EF rowcount/concurrency problem -----
                int orderId;

                var conn = _dbContext.Database.GetDbConnection();
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO orders (user_id, order_date, guest_email) VALUES (@uid, @odate, @gemail); SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmd.CommandType = CommandType.Text;
                    var p1 = cmd.CreateParameter();
                    p1.ParameterName = "@uid";
                    p1.Value = (object?)userId ?? DBNull.Value;
                    p1.DbType = DbType.Int32;
                    cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter();
                    p2.ParameterName = "@odate";
                    p2.Value = DateTime.UtcNow;
                    p2.DbType = DbType.DateTime2;
                    cmd.Parameters.Add(p2);
                    var p3 = cmd.CreateParameter();
                    p3.ParameterName = "@gemail";
                    p3.Value = (object?)guestEmail ?? DBNull.Value;
                    p3.DbType = DbType.String;
                    cmd.Parameters.Add(p3);

                    if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                    // if there is an ambient EF transaction, attach it
                    cmd.Transaction = (_dbContext.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
                    var scalar = await cmd.ExecuteScalarAsync();
                    if (scalar == null || scalar == DBNull.Value)
                    {
                        _loggerInstance.LogError("Failed to create order (raw insert returned null).");
                        return BadRequest("Không thể tạo đơn hàng. Vui lòng thử lại.");
                    }
                    orderId = Convert.ToInt32(scalar);
                }

                // For each cart item: reduce stock and add order detail (EF)
                foreach (var ci in cart)
                {
                    var prod = await _dbContext.Products.FirstOrDefaultAsync(p => p.ProductId == ci.ProductId);
                    if (prod == null)
                    {
                        // unexpected, but handle gracefully
                        _loggerInstance.LogWarning("Mock checkout: product {ProductId} disappeared after order creation.", ci.ProductId);
                        continue;
                    }

                    // reduce stock (basic check)
                    if (prod.Stock < ci.Quantity)
                    {
                        _loggerInstance.LogInformation("Mock checkout: stock changed for product {ProductId} after order creation.", ci.ProductId);
                        continue;
                    }

                    prod.Stock -= ci.Quantity;
                    _dbContext.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = orderId,
                        ProductId = ci.ProductId,
                        Quantity = ci.Quantity,
                        UnitPrice = prod.Price
                    });
                }

                await _dbContext.SaveChangesAsync();

                // Clear cart session
                _httpAccessor.HttpContext?.Session.Remove("Cart");

                _loggerInstance.LogInformation("Mock checkout completed for order {OrderId}", orderId);

                return RedirectToAction("Success", new { id = orderId });
            }
            catch (Exception ex)
            {
                _loggerInstance.LogError(ex, "Unexpected error in mock checkout.");
                return BadRequest("Lỗi khi tạo đơn hàng (mock). Vui lòng thử lại.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            ViewData["OrderId"] = id;

            var totalRow = await _dbContext.VwTotalOrderAmounts.AsNoTracking().FirstOrDefaultAsync(v => v.OrderId == id);
            decimal? orderTotal = totalRow?.Total;

            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? _httpAccessor.HttpContext?.Session.GetString("UserId");
            int? userId = null;
            if (int.TryParse(userIdStr, out var uid)) userId = uid;

            int? userOrders = null;
            if (userId != null)
            {
                userOrders = await _dbContext.Orders.AsNoTracking().CountAsync(o => o.UserId == userId);
            }

            var totalOrders = await ExecuteScalarIntAsync("EXEC pkg_order.sp6", null);
            var totalProducts = await ExecuteScalarIntAsync("EXEC pkg_order.sp7", null);
            var serverNow = await ExecuteScalarDateTimeAsync("EXEC pkg_order.sp8", null);

            var maxPrice = await _dbContext.VwProductLists.AsNoTracking().MaxAsync(p => (decimal?)p.Price);
            var minPrice = await _dbContext.VwProductLists.AsNoTracking().MinAsync(p => (decimal?)p.Price);
            var avgPrice = await _dbContext.VwProductLists.AsNoTracking().AverageAsync(p => (decimal?)p.Price);

            var topProducts = await _dbContext.VwTopSellingProducts.AsNoTracking().Take(5).ToListAsync();

            // order details with product info from vw_OrderDetailsFull
            var rawOrderDetails = await ExecuteReaderToListAsync(
                "SELECT detail_id, order_id, product_id, quantity, name, price FROM vw_OrderDetailsFull WHERE order_id = @id",
                new[] { new SqlParameter("@id", id) });

            var detailsExtended = new List<object>();
            foreach (var od in rawOrderDetails)
            {
                // safe conversions from dictionary values (handle DBNull)
                int productId = od.TryGetValue("product_id", out var pval) && pval != null ? Convert.ToInt32(pval) : 0;
                string name = od.TryGetValue("name", out var nval) && nval != null ? nval.ToString()! : string.Empty;
                int quantity = od.TryGetValue("quantity", out var qval) && qval != null ? Convert.ToInt32(qval) : 0;

                // load product to get category and featured flag (use as fallback for price if view value is invalid)
                var prod = await _dbContext.Products.Include(p => p.Category).AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);

                // determine price robustly
                decimal? price = null;
                if (od.TryGetValue("price", out var prval) && prval != null)
                {
                    switch (prval)
                    {
                        case decimal d: price = d; break;
                        case double db: price = Convert.ToDecimal(db); break;
                        case float f: price = Convert.ToDecimal(f); break;
                        case int i: price = Convert.ToDecimal(i); break;
                        case long l: price = Convert.ToDecimal(l); break;
                        case string s:
                            // Try invariant and Vietnamese formats
                            if (decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out var v1))
                            {
                                price = v1;
                            }
                            else if (decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.GetCultureInfo("vi-VN"), out var v2))
                            {
                                price = v2;
                            }
                            else
                            {
                                // not a numeric string — fall back to product price if available
                                if (prod != null) price = prod.Price;
                                else price = null;
                            }
                            break;
                        default:
                            try
                            {
                                price = Convert.ToDecimal(prval);
                            }
                            catch
                            {
                                if (prod != null) price = prod.Price;
                            }
                            break;
                    }
                }
                else
                {
                    // no price column value — fall back to product price
                    if (prod != null) price = prod.Price;
                }

                // total sold lookup (view may be trusted)
                var topRow = await _dbContext.VwTopSellingProducts.AsNoTracking().FirstOrDefaultAsync(t => t.Name == name);
                int? totalSold = topRow?.TotalSold;

                int? categoryCount = null;
                if (prod?.Category != null)
                {
                    var catRow = await _dbContext.VwCategoryProductCounts.AsNoTracking().FirstOrDefaultAsync(c => c.Name == prod.Category.Name);
                    categoryCount = catRow?.TotalProducts;
                }

                detailsExtended.Add(new
                {
                    ProductId = productId,
                    Name = name,
                    Quantity = quantity,
                    TotalSold = totalSold,
                    IsFeatured = prod?.IsFeatured,
                    CategoryProductCount = categoryCount,
                    Price = price
                });
            }

            ViewData["OrderTotal"] = orderTotal;
            ViewData["UserOrderCount"] = userOrders;
            ViewData["TotalOrders"] = totalOrders;
            ViewData["TotalProducts"] = totalProducts;
            ViewData["ServerNow"] = serverNow;
            ViewData["MaxPrice"] = maxPrice;
            ViewData["MinPrice"] = minPrice;
            ViewData["AvgPrice"] = avgPrice;
            ViewData["OrderDetailsExtended"] = detailsExtended;
            ViewData["TopProducts"] = topProducts;

            return View();
        }

        // New diagnostic endpoint that exercises package/stored-procs/views that are not yet used elsewhere.
        // Accessible to authorized users. Returns a JSON summary (safe, read-only calls).
        [HttpGet]
        public async Task<IActionResult> DbToolbox()
        {
            // List of package procs and stored procs/views that may be unused.
            var results = new Dictionary<string, object?>();
            try
            {
                // pkg_order simple selects
                var pkgNames = new[] { "pkg_order.sp1", "pkg_order.sp2", "pkg_order.sp3", "pkg_order.sp4", "pkg_order.sp5", "pkg_order.sp9", "pkg_order.sp10" };
                foreach (var name in pkgNames)
                {
                    var rows = await ExecuteReaderToListAsync($"EXEC {name}", null);
                    results[name] = new { RowCount = rows.Count, Sample = rows.Take(3).ToList() };
                }

                // some read-only stored procs
                var spRows = await ExecuteReaderToListAsync("EXEC sp_GetProductsByCategory @id", new[] { new SqlParameter("@id", 1) });
                results["sp_GetProductsByCategory(@id=1)"] = new { RowCount = spRows.Count, Sample = spRows.Take(3).ToList() };

                var spRange = await ExecuteReaderToListAsync("EXEC sp_GetProductsByPriceRange @min, @max", new[] { new SqlParameter("@min", 0m), new SqlParameter("@max", 999999m) });
                results["sp_GetProductsByPriceRange(0,999999)"] = new { RowCount = spRange.Count, Sample = spRange.Take(3).ToList() };

                var spSearch = await ExecuteReaderToListAsync("EXEC sp_SearchProduct @key", new[] { new SqlParameter("@key", "a") });
                results["sp_SearchProduct(@key='a')"] = new { RowCount = spSearch.Count, Sample = spSearch.Take(3).ToList() };

                var top = await ExecuteReaderToListAsync("EXEC sp_TopProducts", null);
                results["sp_TopProducts"] = new { RowCount = top.Count, Sample = top.Take(5).ToList() };

                var revenue = await ExecuteScalarDecimalAsync("EXEC sp_TotalRevenue", null);
                results["sp_TotalRevenue"] = revenue;

                // Views: quick counts
                var totalProducts = await _dbContext.VwProductLists.AsNoTracking().CountAsync();
                results["vw_ProductList.Count"] = totalProducts;
                var inStock = await _dbContext.VwInStockProducts.AsNoTracking().CountAsync();
                results["vw_InStockProducts.Count"] = inStock;
                var topSelling = await _dbContext.VwTopSellingProducts.AsNoTracking().Take(5).Select(v => new { v.Name, v.TotalSold }).ToListAsync();
                results["vw_TopSellingProducts.Sample"] = topSelling;
            }
            catch (Exception ex)
            {
                _loggerInstance.LogError(ex, "DbToolbox encountered an error");
                results["error"] = ex.Message;
            }

            return Json(results);
        }

        // Helpers to call scalar DB functions safely and parameterized
        private async Task<decimal?> ExecuteScalarDecimalAsync(string sql, SqlParameter[]? parameters, DbTransaction? transaction = null)
        {
            await using var cmd = _dbContext.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            if (parameters != null) foreach (var p in parameters) cmd.Parameters.Add(p);

            if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();

            var effectiveTx = transaction ?? (_dbContext.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
            if (effectiveTx != null) cmd.Transaction = effectiveTx;

            var obj = await cmd.ExecuteScalarAsync();
            if (obj == null || obj == DBNull.Value) return null;

            // Handle both decimal and other numeric types safely
            if (obj is decimal decimalValue)
                return decimalValue;

            return Convert.ToDecimal(obj);
        }

        private async Task<int?> ExecuteScalarIntAsync(string sql, SqlParameter[]? parameters, DbTransaction? transaction = null)
        {
            await using var cmd = _dbContext.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            if (parameters != null) foreach (var p in parameters) cmd.Parameters.Add(p);

            if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();

            var effectiveTx = transaction ?? (_dbContext.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
            if (effectiveTx != null) cmd.Transaction = effectiveTx;

            var obj = await cmd.ExecuteScalarAsync();
            if (obj == null || obj == DBNull.Value) return null;

            // Handle both int and other numeric types safely
            if (obj is int intValue)
                return intValue;

            return Convert.ToInt32(obj);
        }

        private async Task<DateTime?> ExecuteScalarDateTimeAsync(string sql, SqlParameter[]? parameters, DbTransaction? transaction = null)
        {
            await using var cmd = _dbContext.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            if (parameters != null) foreach (var p in parameters) cmd.Parameters.Add(p);

            if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();

            var effectiveTx = transaction ?? (_dbContext.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
            if (effectiveTx != null) cmd.Transaction = effectiveTx;

            var obj = await cmd.ExecuteScalarAsync();
            if (obj == null || obj == DBNull.Value) return null;

            // Handle both DateTime and other types safely
            if (obj is DateTime dateTimeValue)
                return dateTimeValue;

            return Convert.ToDateTime(obj);
        }

        // New helper: execute a proc or SQL that returns rows and map to list of dictionaries
        private async Task<List<Dictionary<string, object?>>> ExecuteReaderToListAsync(string sql, SqlParameter[]? parameters, DbTransaction? transaction = null)
        {
            var list = new List<Dictionary<string, object?>>();

            var conn = _dbContext.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            if (parameters != null) foreach (var p in parameters) cmd.Parameters.Add(p);

            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            var effectiveTx = transaction ?? (_dbContext.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
            if (effectiveTx != null) cmd.Transaction = effectiveTx;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = val;
                }
                list.Add(row);
            }

            return list;
        }
    }
}