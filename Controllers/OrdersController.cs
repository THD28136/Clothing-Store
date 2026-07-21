using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;
using MTKPM_Clothing_Store_web.Helpers;
using System.Security.Claims;

namespace MTKPM_Clothing_Store_web.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Allowed statuses
        private static readonly string[] AllowedStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

        // Trạng thái còn cho phép hủy = đơn "chưa giao" và chưa vận chuyển.
        // Shipped/Delivered/Cancelled sẽ không cho hủy nữa.
        private static readonly string[] CancellableStatuses = new[] { "Pending", "Processing" };

        // Session key lưu danh sách OrderId mà khách vãng lai đã xác thực (qua GuestLookup)
        private const string GuestVerifiedOrdersSessionKey = "GuestVerifiedOrders";

        public OrdersController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Redirect legacy /Orders/Create to Payments/Checkout
        [HttpGet]
        [AllowAnonymous]
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

        // View order details.
        // Cho phép: Admin, chủ đơn hàng (user đăng nhập), hoặc khách vãng lai đã xác thực qua GuestLookup.
        [HttpGet]
        [AllowAnonymous]
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

            // Lấy UserId từ session/claims
            var userIdString = _httpContextAccessor.HttpContext?.Session.GetString("UserId")
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isOwner = int.TryParse(userIdString, out int userId) && order.UserId == userId;

            bool isGuestVerified = false;
            if (order.UserId == null)
            {
                var verifiedList = HttpContext.Session.GetObject<List<int>>(GuestVerifiedOrdersSessionKey) ?? new List<int>();
                isGuestVerified = verifiedList.Contains(order.OrderId);
            }

            if (!isAdmin && !isOwner && !isGuestVerified)
            {
                // Đơn của khách vãng lai nhưng chưa xác thực -> đưa về trang tra cứu
                if (order.UserId == null)
                {
                    return RedirectToAction(nameof(GuestLookup), new { id = order.OrderId });
                }
                return Forbid();
            }

            // Pass allowed statuses to ViewBag for admin status change UI
            ViewBag.AllowedStatuses = AllowedStatuses;

            var currentStatus = string.IsNullOrWhiteSpace(order.Status) ? "Pending" : order.Status;
            ViewBag.CanCancel = CancellableStatuses.Contains(currentStatus);
            ViewBag.IsGuestOrder = order.UserId == null;

            // Đơn đã giao (Delivered) + là chủ đơn (không phải Admin xem hộ) -> cho phép hiện nút "Đánh giá" từng sản phẩm
            ViewBag.CanReviewThisOrder = isOwner && currentStatus == "Delivered";
            if (ViewBag.CanReviewThisOrder)
            {
                var productIds = order.OrderDetails
                    .Where(od => od.ProductId.HasValue)
                    .Select(od => od.ProductId!.Value)
                    .Distinct()
                    .ToList();
                var reviewedProductIds = await _context.Reviews
                    .Where(r => r.UserId == userId && productIds.Contains(r.ProductId))
                    .Select(r => r.ProductId)
                    .ToListAsync();
                ViewBag.ReviewedProductIds = reviewedProductIds;
            }

            return View(order);
        }

        // POST: Hủy đơn hàng khi đơn chưa giao (Pending/Processing).
        // Cho phép: Admin, chủ đơn hàng, hoặc khách vãng lai đã xác thực qua GuestLookup.
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            var isAdmin = User.IsInRole("Admin");

            var userIdString = _httpContextAccessor.HttpContext?.Session.GetString("UserId")
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isOwner = int.TryParse(userIdString, out int userId) && order.UserId == userId;

            bool isGuestVerified = false;
            if (order.UserId == null)
            {
                var verifiedList = HttpContext.Session.GetObject<List<int>>(GuestVerifiedOrdersSessionKey) ?? new List<int>();
                isGuestVerified = verifiedList.Contains(order.OrderId);
            }

            if (!isAdmin && !isOwner && !isGuestVerified)
            {
                if (order.UserId == null)
                {
                    return RedirectToAction(nameof(GuestLookup), new { id = order.OrderId });
                }
                return Forbid();
            }

            var currentStatus = string.IsNullOrWhiteSpace(order.Status) ? "Pending" : order.Status;
            if (!CancellableStatuses.Contains(currentStatus))
            {
                TempData["Error"] = "Đơn hàng đã được vận chuyển hoặc giao thành công nên không thể hủy.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.Status = "Cancelled";
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đơn hàng #{order.OrderId} đã được hủy thành công.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: form để khách vãng lai nhập Mã đơn hàng + Email/SĐT để tra cứu/hủy đơn
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GuestLookup(int? id)
        {
            if (id.HasValue)
            {
                ViewBag.OrderId = id.Value;
            }
            return View();
        }

        // POST: xác thực khách vãng lai bằng Mã đơn hàng + Email/SĐT
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuestLookup(int orderId, string contact)
        {
            contact = (contact ?? string.Empty).Trim();

            var order = await _context.Orders.FirstOrDefaultAsync(o =>
                o.OrderId == orderId &&
                o.UserId == null &&
                (o.GuestEmail == contact || o.GuestPhone == contact));

            if (order == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy đơn hàng. Vui lòng kiểm tra lại Mã đơn hàng và Email/Số điện thoại đã dùng khi đặt hàng.");
                ViewBag.OrderId = orderId;
                return View();
            }

            var verifiedList = HttpContext.Session.GetObject<List<int>>(GuestVerifiedOrdersSessionKey) ?? new List<int>();
            if (!verifiedList.Contains(order.OrderId))
            {
                verifiedList.Add(order.OrderId);
                HttpContext.Session.SetObject(GuestVerifiedOrdersSessionKey, verifiedList);
            }

            return RedirectToAction(nameof(Details), new { id = order.OrderId });
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