using Microsoft.AspNetCore.Mvc;
using MTKPM_Clothing_Store_web.Models;
using MTKPM_Clothing_Store_web.Helpers;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Adapters;
using System.Linq;

namespace MTKPM_Clothing_Store_web.Controllers
{
    public class CartController : Controller
    {
        private readonly ClothingStoreContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(ClothingStoreContext context)
        {
            _context = context;
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            ViewData["Total"] = cart.Sum(i => i.LineTotal);
            return View(cart);
        }

        // POST: /Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();

            var existing = cart.FirstOrDefault(c => c.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity += Math.Max(1, quantity);
            }
            else
            {
                // Use adapter to convert Product -> CartItem
                var adapter = new ProductToCartItemAdapter(product, quantity);
                cart.Add(adapter.Adapt());
            }

            HttpContext.Session.SetObject(CartSessionKey, cart);

            // If AJAX/XHR request, respond with JSON so client can show a notification and update badge
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var cartCount = cart.Sum(c => c.Quantity);
                return Json(new
                {
                    success = true,
                    message = $"\"{product.Name}\" đã được thêm vào giỏ hàng.",
                    cartCount,
                    total = cart.Sum(i => i.LineTotal)
                });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId, string? returnUrl = null)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.SetObject(CartSessionKey, cart);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var cartCount = cart.Sum(c => c.Quantity);
                var total = cart.Sum(i => i.LineTotal);
                return Json(new { success = true, cartCount, total });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int productId, int quantity)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                HttpContext.Session.SetObject(CartSessionKey, cart);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var cartTotal = cart.Sum(i => i.LineTotal);
                var cartCount = cart.Sum(i => i.Quantity);
                var itemTotal = item?.LineTotal ?? 0m;
                return Json(new { success = true, itemTotal, cartTotal, cartCount });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToAction(nameof(Index));
        }
    }
}