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

        // Allowed statuses
        private static readonly string[] AllowedStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

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

        // Admin: View all orders
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

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

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}