using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        /// <summary>
        /// Main entry point: routes based on authentication and role.
        /// - Admin -> AdminIndex view
        /// - Customer/Guest -> CustomerIndex action
        /// </summary>
        public IActionResult Index()
        {
            // If authenticated and Admin, show admin dashboard
            if (User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                // Return the AdminIndex view directly (no redirect)
                return View("AdminIndex");
            }

            // Otherwise redirect to CustomerIndex for all other users/guests
            return RedirectToAction(nameof(CustomerIndex));
        }

        /// <summary>
        /// Customer-facing home page: load categories with products
        /// Returns View("Index") which displays featured products and category carousels
        /// </summary>
        public async Task<IActionResult> CustomerIndex()
        {
            // Load all categories with their products
            var categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Return the Index view with categories model
            return View("Index", categories);
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
