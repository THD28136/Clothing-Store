using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;
using Newtonsoft.Json;

namespace MTKPM_Clothing_Store_web.Controllers
{
    public class OrderController : Controller
    {
        private readonly ClothingStoreContext _context;

        public OrderController(ClothingStoreContext context)
        {
            _context = context;
        }

        // =========================================
        // 1. Danh sách đơn hàng (User chỉ thấy của mình)
        // =========================================
        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // =========================================
        // 2. Chi tiết đơn hàng
        // =========================================
        public async Task<IActionResult> Details(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // =========================================
        // 3. Trang Checkout (GET)
        // =========================================
        public IActionResult Checkout()
        {
            var cartJson = HttpContext.Session.GetString("Cart");

            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(cartJson);

            return View(cart);
        }

        // =========================================
        // 4. Xử lý Checkout (POST)
        // =========================================
        [HttpPost]
        public async Task<IActionResult> CheckoutConfirm()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            var cartJson = HttpContext.Session.GetString("Cart");

            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(cartJson);

            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Cart");

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                OrderDetails = new List<OrderDetail>()
            };

            decimal total = 0;

            foreach (var item in cart)
            {
                var product = await _context.Products.FindAsync(item.ProductId);

                if (product == null) continue;

                var detail = new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = product.Price
                };

                total += product.Price * item.Quantity;

                order.OrderDetails.Add(detail);
            }

            order.TotalAmount = total;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Xóa giỏ hàng
            HttpContext.Session.Remove("Cart");

            return RedirectToAction("Success", new { id = order.OrderId });
        }

        // =========================================
        // 5. Trang thành công
        // =========================================
        public IActionResult Success(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        // =========================================
        // 6. Xóa đơn hàng
        // =========================================
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order != null)
            {
                var details = _context.OrderDetails.Where(d => d.OrderId == id);
                _context.OrderDetails.RemoveRange(details);

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // 7. Cập nhật trạng thái (Admin)
        // =========================================
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}