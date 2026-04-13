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

namespace MTKPM_Clothing_Store_web.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly ClothingStoreContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(ClothingStoreContext context, IHttpContextAccessor httpContextAccessor, ILogger<PaymentsController> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // GET: show checkout page
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = _httpContextAccessor.HttpContext?.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cart.Any()) return RedirectToAction("Index", "Cart");

            var vm = new CheckoutViewModel();
            foreach (var ci in cart)
            {
                var prod = await _context.Products.FindAsync(ci.ProductId);
                if (prod == null) continue;
                vm.Items.Add(new CheckoutItem { Product = prod, Quantity = ci.Quantity });
            }

            return View(vm);
        }

        // POST: mock payment (school project) with raw-insert Order creation workaround
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutPost()
        {
            var cart = _httpContextAccessor.HttpContext?.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cart.Any()) return RedirectToAction("Index", "Cart");

            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? _httpContextAccessor.HttpContext?.Session.GetString("UserId");

            if (!int.TryParse(userIdStr, out var userId))
            {
                return Challenge();
            }

            try
            {
                _logger.LogInformation("Mock checkout started for user {UserId}. Cart: {@Cart}", userId, cart);

                // Basic pre-check: ensure all products exist and have enough stock
                foreach (var ci in cart)
                {
                    var prod = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == ci.ProductId);
                    if (prod == null)
                    {
                        _logger.LogWarning("Mock checkout: product {ProductId} not found.", ci.ProductId);
                        return BadRequest("Một sản phẩm không tồn tại. Vui lòng kiểm tra giỏ hàng.");
                    }

                    if (prod.Stock < ci.Quantity)
                    {
                        _logger.LogInformation("Mock checkout: insufficient stock for product {ProductId} (need {Need}, have {Have}).", ci.ProductId, ci.Quantity, prod.Stock);
                        return BadRequest($"Không đủ hàng cho sản phẩm '{prod.Name}'. Có {prod.Stock} trong kho.");
                    }
                }

                // ----- WORKAROUND: create order via raw SQL to avoid EF rowcount/concurrency problem -----
                int orderId;
                var now = DateTime.UtcNow;
                await using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO orders (user_id, order_date) VALUES (@uid, @odate); SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmd.CommandType = CommandType.Text;
                    var p1 = cmd.CreateParameter();
                    p1.ParameterName = "@uid";
                    p1.Value = userId;
                    p1.DbType = DbType.Int32;
                    cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter();
                    p2.ParameterName = "@odate";
                    p2.Value = now;
                    p2.DbType = DbType.DateTime2;
                    cmd.Parameters.Add(p2);

                    if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();
                    // if there is an ambient EF transaction, attach it
                    cmd.Transaction = (_context.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
                    var scalar = await cmd.ExecuteScalarAsync();
                    if (scalar == null || scalar == DBNull.Value)
                    {
                        _logger.LogError("Failed to create order (raw insert returned null) for user {UserId}", userId);
                        return BadRequest("Không thể tạo đơn hàng. Vui lòng thử lại.");
                    }
                    orderId = Convert.ToInt32(scalar);
                }

                // For each cart item: reduce stock and add order detail (EF)
                foreach (var ci in cart)
                {
                    var prod = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == ci.ProductId);
                    if (prod == null)
                    {
                        // unexpected, but handle gracefully
                        _logger.LogWarning("Mock checkout: product {ProductId} disappeared after order creation.", ci.ProductId);
                        continue;
                    }

                    // reduce stock (basic check)
                    if (prod.Stock < ci.Quantity)
                    {
                        _logger.LogInformation("Mock checkout: stock changed for product {ProductId} after order creation.", ci.ProductId);
                        continue;
                    }

                    prod.Stock -= ci.Quantity;
                    _context.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = orderId,
                        ProductId = ci.ProductId,
                        Quantity = ci.Quantity
                    });
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException dbuEx)
                {
                    _logger.LogError(dbuEx, "DbUpdateException while saving OrderDetails for Order {OrderId}. Entries: {@Entries}", orderId, dbuEx.Entries.Select(e => new { e.Entity?.GetType().Name, e.State }));
                    if (dbuEx.InnerException != null) _logger.LogError("Inner: {Inner}", dbuEx.InnerException.Message);
                    return BadRequest("Lỗi khi lưu chi tiết đơn hàng (DB). Xem log để biết chi tiết.");
                }

                // Clear cart session
                _httpContextAccessor.HttpContext?.Session.Remove("Cart");

                _logger.LogInformation("Mock checkout completed for order {OrderId}", orderId);

                return RedirectToAction("Success", new { id = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in mock checkout for user {UserId}", userId);
                return BadRequest("Lỗi khi tạo đơn hàng (mock). Vui lòng thử lại hoặc liên hệ giảng viên.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            ViewData["OrderId"] = id;

            // Use views where possible to populate success info (uses the scaffolded view DbSets)
            var totalRow = await _context.VwTotalOrderAmounts.AsNoTracking().FirstOrDefaultAsync(v => v.OrderId == id);
            decimal? orderTotal = totalRow?.Total;

            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? _httpContextAccessor.HttpContext?.Session.GetString("UserId");
            int? userId = null;
            if (int.TryParse(userIdStr, out var uid)) userId = uid;

            int? userOrders = null;
            if (userId != null)
            {
                // number of orders for this user (use Orders table / view if preferred)
                userOrders = await _context.Orders.AsNoTracking().CountAsync(o => o.UserId == userId);
            }

            // totalOrders via pkg_order.sp6 (package stored proc)
            var totalOrders = await ExecuteScalarIntAsync("EXEC pkg_order.sp6", null);

            // product counts via pkg_order.sp7 (package stored proc)
            var totalProducts = await ExecuteScalarIntAsync("EXEC pkg_order.sp7", null);

            // server time via pkg_order.sp8
            var serverNow = await ExecuteScalarDateTimeAsync("EXEC pkg_order.sp8", null);

            // price aggregates via vw_ProductList
            var maxPrice = await _context.VwProductLists.AsNoTracking().MaxAsync(p => (decimal?)p.Price);
            var minPrice = await _context.VwProductLists.AsNoTracking().MinAsync(p => (decimal?)p.Price);
            var avgPrice = await _context.VwProductLists.AsNoTracking().AverageAsync(p => (decimal?)p.Price);

            // top selling products (from view)
            var topProducts = await _context.VwTopSellingProducts.AsNoTracking().Take(5).ToListAsync();

            // order details with product info from vw_OrderDetailsFull
            var orderDetails = await _context.VwOrderDetailsFulls.AsNoTracking().Where(od => od.OrderId == id).ToListAsync();
            var detailsExtended = new List<object>();
            foreach (var od in orderDetails)
            {
                // load product to get category and featured flag
                var prod = await _context.Products.Include(p => p.Category).AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == od.ProductId);

                // total sold can be serviced by vw_TopSellingProducts (match by name). fall back to fn_TotalSold if needed.
                var topRow = await _context.VwTopSellingProducts.AsNoTracking().FirstOrDefaultAsync(t => t.Name == od.Name);
                int? totalSold = topRow?.TotalSold;

                int? categoryCount = null;
                if (prod?.Category != null)
                {
                    var catRow = await _context.VwCategoryProductCounts.AsNoTracking().FirstOrDefaultAsync(c => c.Name == prod.Category.Name);
                    categoryCount = catRow?.TotalProducts;
                }

                detailsExtended.Add(new
                {
                    ProductId = od.ProductId,
                    Name = od.Name,
                    Quantity = od.Quantity,
                    TotalSold = totalSold,
                    IsFeatured = prod?.IsFeatured,
                    CategoryProductCount = categoryCount,
                    Price = od.Price
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
                var totalProducts = await _context.VwProductLists.AsNoTracking().CountAsync();
                results["vw_ProductList.Count"] = totalProducts;
                var inStock = await _context.VwInStockProducts.AsNoTracking().CountAsync();
                results["vw_InStockProducts.Count"] = inStock;
                var topSelling = await _context.VwTopSellingProducts.AsNoTracking().Take(5).Select(v => new { v.Name, v.TotalSold }).ToListAsync();
                results["vw_TopSellingProducts.Sample"] = topSelling;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DbToolbox encountered an error");
                results["error"] = ex.Message;
            }

            return Json(results);
        }

        // Helpers to call scalar DB functions safely and parameterized
        private async Task<decimal?> ExecuteScalarDecimalAsync(string sql, SqlParameter[]? parameters, DbTransaction? transaction = null)
        {
            await using var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            if (parameters != null)
            {
                foreach (var p in parameters) cmd.Parameters.Add(p);
            }

            if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();

            var effectiveTx = transaction ?? (_context.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
            if (effectiveTx != null) cmd.Transaction = effectiveTx;

            var obj = await cmd.ExecuteScalarAsync();
            if (obj == null || obj == DBNull.Value) return null;
            return Convert.ToDecimal(obj);
        }

        private async Task<int?> ExecuteScalarIntAsync(string sql, SqlParameter[]? parameters, DbTransaction? transaction = null)
        {
            await using var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            if (parameters != null)
            {
                foreach (var p in parameters) cmd.Parameters.Add(p);
            }

            if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();

            var effectiveTx = transaction ?? (_context.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
            if (effectiveTx != null) cmd.Transaction = effectiveTx;

            var obj = await cmd.ExecuteScalarAsync();
            if (obj == null || obj == DBNull.Value) return null;
            return Convert.ToInt32(obj);
        }

        private async Task<bool?> ExecuteScalarBoolAsync(string sql, SqlParameter[]? parameters, DbTransaction? transaction = null)
        {
            await using var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            if (parameters != null)
            {
                foreach (var p in parameters) cmd.Parameters.Add(p);
            }

            if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();

            var effectiveTx = transaction ?? (_context.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
            if (effectiveTx != null) cmd.Transaction = effectiveTx;

            var obj = await cmd.ExecuteScalarAsync();
            if (obj == null || obj == DBNull.Value) return null;
            // SQL BIT maps to bool or byte depending on provider; handle both
            if (obj is bool b) return b;
            return Convert.ToInt32(obj) != 0;
        }

        private async Task<DateTime?> ExecuteScalarDateTimeAsync(string sql, SqlParameter[]? parameters, DbTransaction? transaction = null)
        {
            await using var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            if (parameters != null)
            {
                foreach (var p in parameters) cmd.Parameters.Add(p);
            }

            if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();

            var effectiveTx = transaction ?? (_context.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
            if (effectiveTx != null) cmd.Transaction = effectiveTx;

            var obj = await cmd.ExecuteScalarAsync();
            if (obj == null || obj == DBNull.Value) return null;
            return Convert.ToDateTime(obj);
        }

        // New helper: execute a proc or SQL that returns rows and map to list of dictionaries
        private async Task<List<Dictionary<string, object?>>> ExecuteReaderToListAsync(string sql, SqlParameter[]? parameters, DbTransaction? transaction = null)
        {
            var list = new List<Dictionary<string, object?>>();

            await using var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            if (parameters != null)
            {
                foreach (var p in parameters) cmd.Parameters.Add(p);
            }

            if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();

            var effectiveTx = transaction ?? (_context.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
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