using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;
using Microsoft.AspNetCore.Authorization;

namespace MTKPM_Clothing_Store_web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductsController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: Products (customer)
        public async Task<IActionResult> Index(int? categoryId, decimal? minPrice, decimal? maxPrice, string? sort, string? q)
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name");

            IQueryable<Product> query = _context.Products.Include(p => p.Category);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Name.Contains(q) || p.Description.Contains(q));

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.ProductId),
                _ => query.OrderBy(p => p.Name)
            };

            var products = await query.ToListAsync();

            ViewData["sort"] = sort ?? "";
            if (!string.IsNullOrWhiteSpace(q)) ViewData["q"] = q;
            if (categoryId.HasValue) ViewData["categoryId"] = categoryId.Value;
            if (minPrice.HasValue) ViewData["minPrice"] = minPrice.Value;
            if (maxPrice.HasValue) ViewData["maxPrice"] = maxPrice.Value;

            return View(products);
        }

        // GET: Products/AdminIndex (Admin layout)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex(int? categoryId, decimal? minPrice, decimal? maxPrice, string? sort, string? q)
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name");

            IQueryable<Product> query = _context.Products.Include(p => p.Category);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Name.Contains(q) || p.Description.Contains(q));

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.ProductId),
                _ => query.OrderBy(p => p.Name)
            };

            var products = await query.ToListAsync();

            ViewData["sort"] = sort ?? "";
            if (!string.IsNullOrWhiteSpace(q)) ViewData["q"] = q;
            if (categoryId.HasValue) ViewData["categoryId"] = categoryId.Value;
            if (minPrice.HasValue) ViewData["minPrice"] = minPrice.Value;
            if (maxPrice.HasValue) ViewData["maxPrice"] = maxPrice.Value;

            return View("AdminIndex", products);
        }

        // GET: Products/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null)
                return NotFound();

            var reviews = product.Reviews.OrderByDescending(r => r.ReviewDate).ToList();
            ViewBag.Reviews = reviews;
            ViewBag.ReviewCount = reviews.Count;
            ViewBag.AverageRating = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

            var userIdString = _httpContextAccessor.HttpContext?.Session.GetString("UserId")
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out var currentUserId))
            {
                ViewBag.MyReview = reviews.FirstOrDefault(r => r.UserId == currentUserId);

                ViewBag.CanReview = await _context.Orders
                    .Where(o => o.UserId == currentUserId && o.Status != "Cancelled")
                    .AnyAsync(o => o.OrderDetails.Any(od => od.ProductId == product.ProductId));
            }
            else
            {
                ViewBag.CanReview = false;
            }

            return View(product);
        }

        // GET: Products/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("ProductId,Name,Description,Price,CategoryId,Image,IsFeatured,Stock")] Product product, IFormFile? ImageFile)
        {
            ModelState.Remove("Pic"); // Remove old field validation if it exists

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var ext = Path.GetExtension(ImageFile.FileName).ToLower();
                if (ext != ".png")
                {
                    ModelState.AddModelError("ImageFile", "Only .png files accepted");
                }
                else
                {
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                    var fileName = Guid.NewGuid().ToString() + ext;
                    var path = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    product.Image = "images/" + fileName;
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(AdminIndex));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Description,Price,CategoryId,Image,IsFeatured,Stock")] Product product, IFormFile? ImageFile)
        {
            if (id != product.ProductId)
                return NotFound();

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var ext = Path.GetExtension(ImageFile.FileName).ToLower();
                if (ext == ".png")
                {
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                    var fileName = Guid.NewGuid().ToString() + ext;
                    var path = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    product.Image = "images/" + fileName;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(AdminIndex));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(AdminIndex));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}