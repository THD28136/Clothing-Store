using Microsoft.AspNetCore.Mvc;
using MTKPM_Clothing_Store_web.Models;
using Newtonsoft.Json;

public class CartController : Controller
{
    private readonly ClothingStoreContext _context;

    public CartController(ClothingStoreContext context)
    {
        _context = context;
    }

    // =========================
    // Lấy giỏ hàng từ Session
    // =========================
    private List<CartItem> GetCart()
    {
        var cartJson = HttpContext.Session.GetString("Cart");

        return string.IsNullOrEmpty(cartJson)
            ? new List<CartItem>()
            : JsonConvert.DeserializeObject<List<CartItem>>(cartJson);
    }

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
        int count = cart.Sum(x => x.Quantity);
        HttpContext.Session.SetString("CartCount", count.ToString());
    }

    // =========================
    // Thêm vào giỏ
    // =========================
    public IActionResult AddToCart(int productId)
    {
        var cart = GetCart();

        var item = cart.FirstOrDefault(x => x.ProductId == productId);

        if (item != null)
            item.Quantity++;
        else
            cart.Add(new CartItem { ProductId = productId, Quantity = 1 });

        SaveCart(cart);

        return RedirectToAction("Index");
    }

    // =========================
    // Xem giỏ hàng
    // =========================
    public IActionResult Index()
    {
        var cart = GetCart();

        var productIds = cart.Select(c => c.ProductId).ToList();

        var products = _context.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToList();

        var result = cart.Select(c => new
        {
            Product = products.First(p => p.ProductId == c.ProductId),
            c.Quantity
        }).ToList();

        return View(result);
    }

    // =========================
    // Tăng số lượng
    // =========================
    public IActionResult Increase(int productId)
    {
        var cart = GetCart();

        var item = cart.FirstOrDefault(x => x.ProductId == productId);
        if (item != null) item.Quantity++;

        SaveCart(cart);
        return RedirectToAction("Index");
    }

    // =========================
    // Giảm số lượng
    // =========================
    public IActionResult Decrease(int productId)
    {
        var cart = GetCart();

        var item = cart.FirstOrDefault(x => x.ProductId == productId);

        if (item != null)
        {
            item.Quantity--;
            if (item.Quantity <= 0)
                cart.Remove(item);
        }

        SaveCart(cart);
        return RedirectToAction("Index");
    }

    // =========================
    // Xóa sản phẩm
    // =========================
    public IActionResult Remove(int productId)
    {
        var cart = GetCart();

        cart.RemoveAll(x => x.ProductId == productId);

        SaveCart(cart);
        return RedirectToAction("Index");
    }
    
}