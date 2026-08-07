using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Helpers;
using MTKPM_Clothing_Store_web.Models;
using MTKPM_Clothing_Store_web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;
using System.Linq;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace MTKPM_Clothing_Store_web.Controllers
{
    [AllowAnonymous]
    public class PaymentsController : Controller
    {
        // Single, unambiguous DbContext field (ApplicationDbContext is registered in DI)
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpAccessor;
        private readonly ILogger<PaymentsController> _loggerInstance;
        private readonly IEmailService _emailService;
        private readonly ILoyaltyService _loyaltyService;

        public PaymentsController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<PaymentsController> logger, IEmailService emailService, ILoyaltyService loyaltyService)
        {
            this._dbContext = context;
            this._httpAccessor = httpContextAccessor;
            this._loggerInstance = logger;
            this._emailService = emailService;
            this._loyaltyService = loyaltyService;
        }

        // Validates a coupon code against a cart subtotal. Returns the coupon
        // (untracked) if valid, or null + a Vietnamese error message if not.
        // Called on both GET (preview) and POST (authoritative) — POST must
        // never trust a discount value that came from the client.
        private async Task<(Coupon? coupon, string? error)> ValidateCouponAsync(string? code, decimal cartSubtotal)
        {
            if (string.IsNullOrWhiteSpace(code)) return (null, null);

            var normalized = code.Trim();
            var coupon = await _dbContext.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == normalized);
            if (coupon == null) return (null, "Mã giảm giá không hợp lệ.");

            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value.Date < DateTime.UtcNow.Date)
                return (null, "Mã giảm giá đã hết hạn.");

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                return (null, "Mã giảm giá đã hết lượt sử dụng.");

            if (coupon.MinOrderAmount.HasValue && cartSubtotal < coupon.MinOrderAmount.Value)
                return (null, $"Đơn hàng phải từ {coupon.MinOrderAmount.Value:C0} trở lên để áp dụng mã này.");

            return (coupon, null);
        }

        // GET: show checkout page
        [HttpGet]
        public async Task<IActionResult> Checkout(string? couponCode = null, int? selectedAddressId = null)
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

                // Nạp sổ địa chỉ của user để chọn lúc checkout (giống Shopee).
                vm.Addresses = await _dbContext.ShippingAddresses
                    .Where(a => a.UserId == uid)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.CreatedAt)
                    .ToListAsync();

                // If caller provided a selectedAddressId, preselect it; otherwise leave DefaultAddress as before
                if (selectedAddressId.HasValue && vm.Addresses.Any(a => a.AddressId == selectedAddressId.Value))
                {
                    vm.SelectedAddressId = selectedAddressId.Value;
                }

                // Khách hàng thân thiết: hạng thành viên + điểm khả dụng để hiển thị lúc checkout.
                var tierInfo = await _loyaltyService.GetTierInfoAsync(uid);
                vm.TierName = tierInfo.TierName;
                vm.TierDiscountPercent = tierInfo.DiscountPercent;
                vm.AvailablePoints = await _loyaltyService.GetAvailablePointsAsync(uid);
            }

            // default payment method remains Card; you can override with querystring if needed
            vm.SelectedPaymentMethod = PaymentMethodType.Card;

            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var (coupon, error) = await ValidateCouponAsync(couponCode, vm.Total);
                vm.CouponCode = couponCode;
                if (coupon != null)
                {
                    vm.AppliedDiscountPercent = coupon.DiscountPercent;
                }
                else
                {
                    vm.CouponMessage = error;
                }
            }

            return View(vm);
        }

        // POST: Apply coupon from the summary form. Redirect to Checkout GET which does the validation and shows feedback.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyCoupon(string? couponCode)
        {
            // Redirect to the Checkout GET so it will compute totals and validate the coupon.
            return RedirectToAction("Checkout", new { couponCode = couponCode });
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

            // If the user did not choose Card, remove card-related modelstate so validation won't require them
            if (model.SelectedPaymentMethod != PaymentMethodType.Card)
            {
                ModelState.Remove(nameof(model.CardName));
                ModelState.Remove(nameof(model.CardNumber));
                ModelState.Remove(nameof(model.ExpiryMonth));
                ModelState.Remove(nameof(model.ExpiryYear));
                ModelState.Remove(nameof(model.CVV));
            }

            // model validation
            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            // If card chosen, validate expiry + basic card number
            if (model.SelectedPaymentMethod == PaymentMethodType.Card)
            {
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
            }

            // Ensure we have the current user id (prefer claim, fallback to session)
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? _httpAccessor.HttpContext?.Session.GetString("UserId");
            int? userId = null;
            if (int.TryParse(userIdStr, out var uid)) userId = uid;

            // Guest checkout: no account, so require a name + email instead of a login
            string? guestEmail = null;
            string? guestName = null;
            if (userId == null)
            {
                guestEmail = model.GuestEmail?.Trim();
                if (string.IsNullOrWhiteSpace(guestEmail))
                {
                    ModelState.AddModelError(nameof(model.GuestEmail), "Vui lòng nhập email để nhận xác nhận đơn hàng.");
                    return View("Checkout", model);
                }

                guestName = model.GuestName?.Trim();
                if (string.IsNullOrWhiteSpace(guestName))
                {
                    ModelState.AddModelError(nameof(model.GuestName), "Vui lòng nhập họ tên.");
                    return View("Checkout", model);
                }

                if (string.IsNullOrWhiteSpace(model.GuestProvince) || string.IsNullOrWhiteSpace(model.GuestWard) || string.IsNullOrWhiteSpace(model.GuestStreet))
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ địa chỉ nhận hàng.");
                    return View("Checkout", model);
                }
            }
            else
            {
                // User đã đăng nhập: bắt buộc phải chọn 1 địa chỉ trong sổ địa chỉ của chính mình.
                if (!model.SelectedAddressId.HasValue)
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng chọn địa chỉ giao hàng.");
                    model.Addresses = await _dbContext.Set<ShippingAddress>().Where(a => a.UserId == userId.Value).ToListAsync();
                    return View("Checkout", model);
                }

                var ownsAddress = await _dbContext.ShippingAddresses.AnyAsync(a => a.AddressId == model.SelectedAddressId.Value && a.UserId == userId.Value);
                if (!ownsAddress)
                {
                    ModelState.AddModelError(string.Empty, "Địa chỉ giao hàng không hợp lệ.");
                    model.Addresses = await _dbContext.Set<ShippingAddress>().Where(a => a.UserId == userId.Value).ToListAsync();
                    return View("Checkout", model);
                }
            }
            model.UserId = userId;

            // Khách hàng thân thiết: tính lại hạng + điểm PHÍA SERVER (không tin giá trị từ client).
            if (userId != null)
            {
                var tierInfo = await _loyaltyService.GetTierInfoAsync(userId.Value);
                model.TierName = tierInfo.TierName;
                model.TierDiscountPercent = tierInfo.DiscountPercent;

                var availablePoints = await _loyaltyService.GetAvailablePointsAsync(userId.Value);
                model.AvailablePoints = availablePoints;

                if (model.PointsToRedeem > 0)
                {
                    if (model.PointsToRedeem > availablePoints)
                    {
                        ModelState.AddModelError(nameof(model.PointsToRedeem), "Bạn không đủ điểm để sử dụng.");
                        model.Addresses = await _dbContext.Set<ShippingAddress>().Where(a => a.UserId == userId.Value).ToListAsync();
                        return View("Checkout", model);
                    }

                    // Giới hạn: điểm dùng tối đa quy đổi không quá 50% giá trị đơn hàng gốc.
                    var maxPointsDiscount = model.Total * 0.5m;
                    if (model.PointsToRedeem * 1000m > maxPointsDiscount)
                    {
                        ModelState.AddModelError(nameof(model.PointsToRedeem), $"Chỉ được dùng tối đa {(int)(maxPointsDiscount / 1000m)} điểm cho đơn hàng này (không quá 50% giá trị đơn).");
                        model.Addresses = await _dbContext.Set<ShippingAddress>().Where(a => a.UserId == userId.Value).ToListAsync();
                        return View("Checkout", model);
                    }
                }
            }
            else
            {
                // Khách vãng lai không có tài khoản -> không có điểm/hạng.
                model.TierName = "Thân thiết";
                model.TierDiscountPercent = 0;
                model.AvailablePoints = 0;
                model.PointsToRedeem = 0;
            }

            // Re-validate the coupon here, authoritively — model.AppliedDiscountPercent
            // from the GET preview is never trusted for the actual charge/order.
            Coupon? appliedCoupon = null;

            if (!string.IsNullOrWhiteSpace(model.CouponCode))
            {
                var (coupon, error) = await ValidateCouponAsync(model.CouponCode, model.Total);

                if (coupon == null)
                {
                    model.CouponMessage = error;
                    ModelState.AddModelError(nameof(model.CouponCode), error ?? "Mã giảm giá không hợp lệ.");
                    return View("Checkout", model);
                }

                appliedCoupon = coupon;
                model.AppliedDiscountPercent = coupon.DiscountPercent;
            }

            var discountAmount = model.DiscountAmount;
            var finalTotal = model.FinalTotal;

            // Map selected payment method to an id stored in Order.PaymentMethodId
            int paymentMethodId = model.SelectedPaymentMethod switch
            {
                PaymentMethodType.Card => 1,
                PaymentMethodType.COD => 2,
                PaymentMethodType.PayPal => 3,
                PaymentMethodType.Momo => 4,
                _ => 1
            };

            try
            {
                _loggerInstance.LogInformation("Checkout started for user {UserId}. PaymentMethod={PaymentMethod}. Cart: {@Cart}", userId, model.SelectedPaymentMethod, cart);

                // Basic pre-check: ensure all products exist and have enough stock
                foreach (var ci in cart)
                {
                    var prod = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == ci.ProductId);
                    if (prod == null)
                    {
                        _loggerInstance.LogWarning("Checkout: product {ProductId} not found.", ci.ProductId);
                        return BadRequest("Một sản phẩm không tồn tại. Vui lòng kiểm tra giỏ hàng.");
                    }

                    if (prod.Stock < ci.Quantity)
                    {
                        _loggerInstance.LogInformation("Checkout: insufficient stock for product {ProductId} (need {Need}, have {Have}).", ci.ProductId, ci.Quantity, prod.Stock);
                        return BadRequest($"Không đủ hàng cho sản phẩm '{prod.Name}'. Có {prod.Stock} trong kho.");
                    }
                }

                // Atomically claim one use of the coupon (if any)
                if (appliedCoupon != null)
                {
                    var couponConn = _dbContext.Database.GetDbConnection();
                    if (couponConn.State != ConnectionState.Open) await couponConn.OpenAsync();
                    await using var incCmd = couponConn.CreateCommand();
                    incCmd.CommandText = "UPDATE coupons SET used_count = used_count + 1 WHERE coupon_id = @cid AND (usage_limit IS NULL OR used_count < usage_limit)";
                    incCmd.CommandType = CommandType.Text;
                    incCmd.Transaction = (_dbContext.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
                    var pcid = incCmd.CreateParameter();
                    pcid.ParameterName = "@cid";
                    pcid.Value = appliedCoupon.CouponId;
                    pcid.DbType = DbType.Int32;
                    incCmd.Parameters.Add(pcid);

                    var rowsAffected = await incCmd.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        _loggerInstance.LogInformation("Coupon {Code} hit its usage limit at checkout time.", appliedCoupon.Code);
                        ModelState.AddModelError(nameof(model.CouponCode), "Mã giảm giá vừa hết lượt sử dụng. Vui lòng bỏ mã và thử lại.");
                        return View("Checkout", model);
                    }
                }

                // ----- create order via raw SQL (include payment_method_id) -----
                int orderId;
                bool isPayPal = model.SelectedPaymentMethod == PaymentMethodType.PayPal;

                var conn = _dbContext.Database.GetDbConnection();
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"INSERT INTO orders
                    (
                        user_id,
                        order_date,
                        status,
                        guest_email,
                        guest_name,
                        guest_phone,
                        address_id,
                        guest_province,
                        guest_ward,
                        guest_street,
                        coupon_id,
                        payment_method_id,
                        total_amount,
                        points_redeemed,
                        points_discount_amount
                    )
                    VALUES
                    (
                        @uid,
                        @odate,
                        @status,
                        @gemail,
                        @gname,
                        @gphone,
                        @addrid,
                        @gprovince,
                        @gward,
                        @gstreet,
                        @cid,
                        @pmid,
                        @total,
                        @pointsRedeemed,
                        @pointsDiscount
                    );

                    SELECT CAST(SCOPE_IDENTITY() AS INT);";
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
                    var pgn = cmd.CreateParameter();
                    pgn.ParameterName = "@gname";
                    pgn.Value = (object?)guestName ?? DBNull.Value;
                    pgn.DbType = DbType.String;
                    cmd.Parameters.Add(pgn);
                    var pphone = cmd.CreateParameter();
                    pphone.ParameterName = "@gphone";
                    pphone.Value = (object?)model.GuestPhone ?? DBNull.Value;
                    pphone.DbType = DbType.String;
                    cmd.Parameters.Add(pphone);
                    var paddrid = cmd.CreateParameter();
                    paddrid.ParameterName = "@addrid";
                    paddrid.Value = (object?)model.SelectedAddressId ?? DBNull.Value;
                    paddrid.DbType = DbType.Int32;
                    cmd.Parameters.Add(paddrid);
                    var pgprov = cmd.CreateParameter();
                    pgprov.ParameterName = "@gprovince";
                    pgprov.Value = (object?)model.GuestProvince?.Trim() ?? DBNull.Value;
                    pgprov.DbType = DbType.String;
                    cmd.Parameters.Add(pgprov);
                    var pgward = cmd.CreateParameter();
                    pgward.ParameterName = "@gward";
                    pgward.Value = (object?)model.GuestWard?.Trim() ?? DBNull.Value;
                    pgward.DbType = DbType.String;
                    cmd.Parameters.Add(pgward);
                    var pgstreet = cmd.CreateParameter();
                    pgstreet.ParameterName = "@gstreet";
                    pgstreet.Value = (object?)model.GuestStreet?.Trim() ?? DBNull.Value;
                    pgstreet.DbType = DbType.String;
                    cmd.Parameters.Add(pgstreet);
                    var p4 = cmd.CreateParameter();
                    p4.ParameterName = "@cid";
                    p4.Value = (object?)appliedCoupon?.CouponId ?? DBNull.Value;
                    p4.DbType = DbType.Int32;
                    cmd.Parameters.Add(p4);
                    var p5 = cmd.CreateParameter();
                    p5.ParameterName = "@pmid";
                    p5.Value = paymentMethodId;
                    p5.DbType = DbType.Int32;
                    cmd.Parameters.Add(p5);
                    var p6 = cmd.CreateParameter();
                    p6.ParameterName = "@total";
                    p6.Value = finalTotal;
                    p6.DbType = DbType.Decimal;
                    cmd.Parameters.Add(p6);
                    var p7 = cmd.CreateParameter();
                    p7.ParameterName = "@status";
                    p7.Value = isPayPal ? "PendingPayment" : "Pending";
                    p7.DbType = DbType.String;
                    cmd.Parameters.Add(p7);
                    var pPointsRedeemed = cmd.CreateParameter();
                    pPointsRedeemed.ParameterName = "@pointsRedeemed";
                    pPointsRedeemed.Value = model.PointsToRedeem > 0 ? (object)model.PointsToRedeem : DBNull.Value;
                    pPointsRedeemed.DbType = DbType.Int32;
                    cmd.Parameters.Add(pPointsRedeemed);
                    var pPointsDiscount = cmd.CreateParameter();
                    pPointsDiscount.ParameterName = "@pointsDiscount";
                    pPointsDiscount.Value = model.PointsToRedeem > 0 ? (object)model.PointsDiscountAmount : DBNull.Value;
                    pPointsDiscount.DbType = DbType.Decimal;
                    cmd.Parameters.Add(pPointsDiscount);

                    if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                    cmd.Transaction = (_dbContext.Database.CurrentTransaction as RelationalTransaction)?.GetDbTransaction();
                    var scalar = await cmd.ExecuteScalarAsync();
                    if (scalar == null || scalar == DBNull.Value)
                    {
                        _loggerInstance.LogError("Failed to create order (raw insert returned null).");
                        return BadRequest("Không thể tạo đơn hàng. Vui lòng thử lại.");
                    }
                    orderId = Convert.ToInt32(scalar);
                }

                // For PayPal: do not deduct stock now — create order details only and mark PendingPayment.
                // For other methods: reduce stock and add order detail (existing behaviour).

                foreach (var ci in cart)
                {
                    var prod = await _dbContext.Products.FirstOrDefaultAsync(p => p.ProductId == ci.ProductId);
                    if (prod == null)
                    {
                        _loggerInstance.LogWarning("Checkout: product {ProductId} disappeared after order creation.", ci.ProductId);
                        continue;
                    }

                    if (!isPayPal)
                    {
                        if (prod.Stock < ci.Quantity)
                        {
                            _loggerInstance.LogInformation("Checkout: stock changed for product {ProductId} after order creation.", ci.ProductId);
                            continue;
                        }

                        prod.Stock -= ci.Quantity;
                    }
                    else
                    {
                        // For PayPal we keep stock unchanged until capture; still check availability earlier already ensured.
                    }

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
                if (!isPayPal)
                {
                    _httpAccessor.HttpContext?.Session.Remove("Cart");
                }

                _loggerInstance.LogInformation("Checkout completed for order {OrderId}", orderId);
                await _dbContext.SaveChangesAsync();

                // Khách hàng thân thiết: trừ điểm đã dùng (nếu có) ngay sau khi đơn tạo thành công,
                // TRƯỚC nhánh PayPal — nếu không, đơn PayPal vẫn được giảm giá theo điểm nhưng
                // điểm lại không bị trừ (lỗ hổng cho phép giảm giá miễn phí).
                if (userId != null && model.PointsToRedeem > 0)
                {
                    await _loyaltyService.RedeemPointsAsync(userId.Value, model.PointsToRedeem, orderId, $"Dùng điểm giảm giá cho đơn hàng #{orderId}");
                }

                // If PayPal chosen, redirect into PayPal flow controller which will create PayPal order and redirect customer.
                if (isPayPal)
                {
                    return RedirectToAction("Create", "PaymentsPayPal", new { orderId = orderId });
                }

                // Gửi email xác nhận đơn hàng (không chặn luồng nếu gửi thất bại).
                var recipientEmail = guestEmail;
                var recipientName = guestName;
                if (userId != null)
                {
                    var accountUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId.Value);
                    recipientEmail = accountUser?.Email;
                    recipientName = accountUser?.Name;
                }

                if (!string.IsNullOrWhiteSpace(recipientEmail))
                {
                    var emailBody = BuildOrderConfirmationEmail(orderId, recipientName, model.Items, finalTotal, model.SelectedPaymentMethod.ToString());
                    await _emailService.SendEmailAsync(recipientEmail!, $"Xác nhận đơn hàng #{orderId} - LOVStore", emailBody);
                }

                // For other methods we treat as immediate success (existing behaviour)
                return RedirectToAction("Success", new { id = orderId });
            }
            catch (Exception ex)
            {
                _loggerInstance.LogError(ex, "Unexpected error in checkout.");
                return BadRequest("Lỗi khi tạo đơn hàng. Vui lòng thử lại.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            ViewData["OrderId"] = id;

            var totalRow = await _dbContext.VwTotalOrderAmounts.AsNoTracking().FirstOrDefaultAsync(v => v.OrderId == id);
            var storedOrder = await _dbContext.Orders.AsNoTracking()
                .Include(o => o.Coupon)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            // total_amount is only populated by CheckoutPost as of this change (it
            // reflects any coupon discount); vw_TotalOrderAmounts sums the raw
            // order_details and knows nothing about discounts, so prefer the
            // stored value whenever it's present.
            decimal? orderTotal = storedOrder?.TotalAmount ?? totalRow?.Total;
            ViewData["AppliedCoupon"] = storedOrder?.Coupon;

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
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

            try
            {

                transaction = await _dbContext.Database.BeginTransactionAsync();

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

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _loggerInstance.LogError(ex, "DbToolbox encountered an error");
                results["error"] = ex.Message;
                if (transaction != null)
                    await transaction.RollbackAsync();
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
        // Xây dựng nội dung email HTML xác nhận đơn hàng.
        private string BuildOrderConfirmationEmail(int orderId, string? customerName, List<CheckoutItem> items, decimal total, string paymentMethod)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>");
            sb.Append($"<h2>Cảm ơn {(string.IsNullOrWhiteSpace(customerName) ? "bạn" : customerName)} đã đặt hàng!</h2>");
            sb.Append($"<p>Đơn hàng <strong>#{orderId}</strong> của bạn đã được ghi nhận thành công.</p>");
            sb.Append("<table style='width:100%;border-collapse:collapse;margin-top:12px;'>");
            sb.Append("<thead><tr style='background:#f5f5f5;'>" +
                      "<th style='text-align:left;padding:8px;border:1px solid #ddd;'>Sản phẩm</th>" +
                      "<th style='text-align:center;padding:8px;border:1px solid #ddd;'>SL</th>" +
                      "<th style='text-align:right;padding:8px;border:1px solid #ddd;'>Đơn giá</th>" +
                      "<th style='text-align:right;padding:8px;border:1px solid #ddd;'>Thành tiền</th></tr></thead><tbody>");

            foreach (var item in items)
            {
                var subtotal = item.SubTotal;
                sb.Append("<tr>");
                sb.Append($"<td style='padding:8px;border:1px solid #ddd;'>{item.Product?.Name}</td>");
                sb.Append($"<td style='text-align:center;padding:8px;border:1px solid #ddd;'>{item.Quantity}</td>");
                sb.Append($"<td style='text-align:right;padding:8px;border:1px solid #ddd;'>{item.Product?.Price:N0}₫</td>");
                sb.Append($"<td style='text-align:right;padding:8px;border:1px solid #ddd;'>{subtotal:N0}₫</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
            sb.Append($"<p style='margin-top:12px;font-size:16px;'><strong>Tổng cộng: {total:N0}₫</strong></p>");
            sb.Append($"<p><strong>Phương thức thanh toán:</strong> {paymentMethod}</p>");
            sb.Append("<p>Chúng tôi sẽ thông báo khi đơn hàng được xử lý và giao đi. Cảm ơn bạn đã mua sắm tại MTKPM Clothing Store!</p>");
            sb.Append("</div>");

            return sb.ToString();
        }
    }
}