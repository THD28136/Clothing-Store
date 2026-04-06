using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using MTKPM_Clothing_Store_web.Models;

namespace MTKPM_Clothing_Store_web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ClothingStoreContext _context;

        public HomeController(ILogger<HomeController> logger, ClothingStoreContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Nếu đã đăng nhập, chuyển theo vai trò
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    // Hiển thị dashboard admin (Views/Home/Index.cshtml)
                    return View();
                }

                // Người dùng không phải Admin -> chuyển tới trang khách
                return RedirectToAction("CustomerIndex");
            }

            // Khách -> trang khách
            return RedirectToAction("CustomerIndex");
        }

        // Trang khách hàng: tải danh mục + sản phẩm
        public async Task<IActionResult> CustomerIndex()
        {
            // Nạp toàn bộ categories cùng products. Ở view chỉ hiển thị một số sản phẩm (Take).
            var categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
