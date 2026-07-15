using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;

namespace MTKPM_Clothing_Store_web.Controllers
{
    [Authorize]  // Bắt buộc đăng nhập cho tất cả action
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

        // Customer: View their own orders (fixed role check)
        [HttpGet]
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

            // Lấy UserId từ session/claims
            var userIdString = _httpContextAccessor.HttpContext?.Session.GetString("UserId")
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            // Nếu không phải Admin, chỉ xem được đơn hàng của mình
            if (!isAdmin && int.TryParse(userIdString, out int userId))
            {
                if (order.UserId != userId)
                {
                    return Forbid();
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