using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;

namespace MTKPM_Clothing_Store_web.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Allowed statuses (including payment-related statuses used by PayPal)
        private static readonly string[] AllowedStatuses = new[] { "Pending", "PendingPayment", "Paid", "Processing", "Shipped", "Delivered", "Cancelled" };

        // Valid state transitions (from -> allowed next states).
        // Keep this small and explicit so UpdateStatus can reject illogical changes.
        private static readonly Dictionary<string, string[]> ValidTransitions = new()
        {
            [""] = new[] { "Pending", "PendingPayment" }, // unknown/empty -> initial states
            ["Pending"] = new[] { "PendingPayment", "Paid", "Processing", "Cancelled" },
            ["PendingPayment"] = new[] { "Paid", "Cancelled" },
            ["Paid"] = new[] { "Processing", "Cancelled" },
            ["Processing"] = new[] { "Shipped", "Cancelled" },
            ["Shipped"] = new[] { "Delivered" }, // typically once shipped only Delivered is next
            ["Delivered"] = Array.Empty<string>(), // terminal
            ["Cancelled"] = Array.Empty<string>()  // terminal in current model (you handle restore elsewhere)
        };

        private static bool IsValidTransition(string? from, string to)
        {
            var key = string.IsNullOrWhiteSpace(from) ? "" : from!;
            if (!ValidTransitions.TryGetValue(key, out var allowed)) return false;
            return allowed.Contains(to);
        }

        public OrdersController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Redirect legacy /Orders/Create to Payments/Checkout
        [HttpGet]
        public IActionResult Create()
        {
            return RedirectToAction("Checkout", "Payments");
        }

        // Guest: form to look up an order by order number + email
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Track()
        {
            return View();
        }

        // Guest: verify order number + email match, then let them into Details
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Track(int orderId, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập email.");
                return View();
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            var order = await _context.Orders.FirstOrDefaultAsync(o =>
                o.OrderId == orderId &&
                o.UserId == null &&
                o.GuestEmail != null &&
                o.GuestEmail.ToLower() == normalizedEmail);

            if (order == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy đơn hàng với mã số và email này.");
                return View();
            }

            // Mark this browser session as having proven ownership of this
            // specific order id, so Details() can allow it through below.
            _httpContextAccessor.HttpContext?.Session.SetInt32("VerifiedGuestOrderId", orderId);

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // Admin: View all orders (with search and status filter)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string? search, string? status)
        {
            var q = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmed = search.Trim();
                // if numeric, allow searching by order id
                if (int.TryParse(trimmed, out var oid))
                {
                    q = q.Where(o => o.OrderId == oid
                                     || (o.User != null && EF.Functions.Like(o.User.Name, $"%{trimmed}%"))
                                     || (o.GuestEmail != null && EF.Functions.Like(o.GuestEmail, $"%{trimmed}%")));
                }
                else
                {
                    q = q.Where(o => (o.User != null && EF.Functions.Like(o.User.Name, $"%{trimmed}%"))
                                     || (o.GuestEmail != null && EF.Functions.Like(o.GuestEmail, $"%{trimmed}%")));
                }
            }

            if (!string.IsNullOrWhiteSpace(status) && AllowedStatuses.Contains(status))
            {
                q = q.Where(o => o.Status == status);
            }

            var orders = await q.OrderByDescending(o => o.OrderDate).ToListAsync();

            ViewBag.AllowedStatuses = AllowedStatuses;
            ViewBag.Search = search;
            ViewBag.StatusFilter = status;

            return View(orders);
        }

        // Customer: View their own orders (any logged-in account)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UserOrders()
        {
            // Lấy UserId từ session hoặc claim
            var userIdString = _httpContextAccessor.HttpContext?.Session.GetString("UserId")
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login", "Users");
            }

            // Kiểm tra role: hoặc từ session hoặc từ claims
            var userRole = _httpContextAccessor.HttpContext?.Session.GetString("Role")
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? (User?.IsInRole("Admin") == true ? "Admin" : null);

            // Nếu không phải Admin, kiểm tra chỉ là customer/user
            if (userRole != "Admin" && !new[] { "User", "customer" }.Contains(userRole ?? ""))
            {
                return Forbid();
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // View order details (protected by [Authorize] at class level)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Coupon)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                if (order.UserId != null)
                {
                    // This order belongs to a registered account: caller must be
                    // logged in AND be that same user. (Previously, if the
                    // caller wasn't logged in, int.TryParse failed and this
                    // whole check was skipped — silently letting anyone view
                    // any account's order by guessing the id.)
                    var userIdString = _httpContextAccessor.HttpContext?.Session.GetString("UserId")
                        ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                    if (!int.TryParse(userIdString, out int userId) || order.UserId != userId)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    // Guest order (no account). Only allow viewing if this
                    // browser session just verified ownership via Track()
                    // with the matching order id + email.
                    var verifiedOrderId = _httpContextAccessor.HttpContext?.Session.GetInt32("VerifiedGuestOrderId");
                    if (verifiedOrderId != id)
                    {
                        return Forbid();
                    }
                }
            }

            // Pass allowed statuses to ViewBag for admin status change UI
            ViewBag.AllowedStatuses = AllowedStatuses;

            return View(order);
        }

        // Admin-only: update order status
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            if (!AllowedStatuses.Contains(status))
            {
                ModelState.AddModelError(string.Empty, "Invalid status.");
                return RedirectToAction(nameof(Details), new { id });
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound();

            if (!IsValidTransition(order.Status, status))
            {
                ModelState.AddModelError(string.Empty, $"Invalid status transition from '{order.Status ?? "None"}' to '{status}'.");
                return RedirectToAction(nameof(Details), new { id });
            }

            var wasCancelled = order.Status == "Cancelled";
            var isNowCancelled = status == "Cancelled";

            if (isNowCancelled && !wasCancelled)
            {
                // Order is being cancelled for the first time: give the stock back.
                foreach (var detail in order.OrderDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.Stock += detail.Quantity;
                    }
                }
            }
            else if (!isNowCancelled && wasCancelled)
            {
                // Symmetric case: admin reverses a cancellation back to an
                // active status. Re-deduct the stock we gave back above, but
                // refuse if there isn't enough left.
                foreach (var detail in order.OrderDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null && product.Stock < detail.Quantity)
                    {
                        ModelState.AddModelError(string.Empty, $"Không đủ hàng tồn kho cho sản phẩm '{product.Name}' để khôi phục đơn hàng.");
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                foreach (var detail in order.OrderDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.Stock -= detail.Quantity;
                    }
                }
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // Admin-only: bulk update status for selected orders
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateStatus([FromForm] int[]? selectedIds, [FromForm] string? status)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(status) || !AllowedStatuses.Contains(status))
            {
                TempData["Error"] = "Trạng thái không hợp lệ cho thao tác hàng loạt.";
                return RedirectToAction(nameof(Index));
            }

            // Start transaction to keep updates consistent
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var orders = await _context.Orders
                    .Where(o => selectedIds.Contains(o.OrderId))
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .ToListAsync();

                foreach (var order in orders)
                {
                    var wasCancelled = order.Status == "Cancelled";
                    var isNowCancelled = status == "Cancelled";

                    if (isNowCancelled && !wasCancelled)
                    {
                        // give stock back
                        foreach (var detail in order.OrderDetails)
                        {
                            var product = await _context.Products.FindAsync(detail.ProductId);
                            if (product != null)
                            {
                                product.Stock += detail.Quantity;
                            }
                        }
                    }
                    else if (!isNowCancelled && wasCancelled)
                    {
                        // Check availability first
                        foreach (var detail in order.OrderDetails)
                        {
                            var product = await _context.Products.FindAsync(detail.ProductId);
                            if (product != null && product.Stock < detail.Quantity)
                            {
                                TempData["Error"] = $"Không đủ hàng tồn kho cho sản phẩm '{product.Name}' khi khôi phục đơn #{order.OrderId}. Hủy thao tác.";
                                await tx.RollbackAsync();
                                return RedirectToAction(nameof(Index));
                            }
                        }

                        foreach (var detail in order.OrderDetails)
                        {
                            var product = await _context.Products.FindAsync(detail.ProductId);
                            if (product != null)
                            {
                                product.Stock -= detail.Quantity;
                            }
                        }
                    }

                    order.Status = status;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = $"Cập nhật trạng thái thành công cho {orders.Count} đơn hàng.";
            }
            catch (Exception)
            {
                try { await tx.RollbackAsync(); } catch { }
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật trạng thái hàng loạt. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Allow authenticated customers (owners) to cancel their own order.
        // Admins can still use UpdateStatus.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            var isAdmin = User.IsInRole("Admin");

            // If not admin, ensure the caller is the owner of the order
            if (!isAdmin)
            {
                var userIdString = _httpContextAccessor.HttpContext?.Session.GetString("UserId")
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdString, out int userId) || order.UserId != userId)
                {
                    return Forbid();
                }
            }

            var wasCancelled = order.Status == "Cancelled";
            if (!wasCancelled)
            {
                // Return stock for each item once
                foreach (var detail in order.OrderDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.Stock += detail.Quantity;
                    }
                }

                order.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}