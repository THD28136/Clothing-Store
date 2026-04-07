using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;
using Microsoft.AspNetCore.Authorization;
using MTKPM_Clothing_Store_web.Services;

namespace MTKPM_Clothing_Store_web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ClothingStoreContext _context;

        public ProductsController(ClothingStoreContext context)
        {
            _context = context;
        }

        // GET: Products (customer)
        public async Task<IActionResult> Index(int? categoryId, decimal? minPrice, decimal? maxPrice, string? sort, string? q)
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name");

            // Use facade service + strategy internally
            var service = new ProductService(_context);
            var products = await service.GetProductsAsync(categoryId, minPrice, maxPrice, sort, q);

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

            var service = new ProductService(_context);
            var products = await service.GetProductsAsync(categoryId, minPrice, maxPrice, sort, q);

            ViewData["sort"] = sort ?? "";
            if (!string.IsNullOrWhiteSpace(q)) ViewData["q"] = q;
            if (categoryId.HasValue) ViewData["categoryId"] = categoryId.Value;
            if (minPrice.HasValue) ViewData["minPrice"] = minPrice.Value;
            if (maxPrice.HasValue) ViewData["maxPrice"] = maxPrice.Value;

            return View("AdminIndex", products);
        }

        // POST: Products/ToggleFeatured/ (Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleFeatured(int id, string? returnUrl = null)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsFeatured = !product.IsFeatured;
            _context.Update(product);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(AdminIndex));
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

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
        public async Task<IActionResult> Create([Bind("ProductId,Name,Description,Price,CategoryId,IsFeatured")] Product product, IFormFile? ImageFile)
        {
            ModelState.Remove("Pic");

            // image handling remains identical (keeps existing behavior)
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var ext = Path.GetExtension(ImageFile.FileName).ToLower();
                if (ext != ".png")
                {
                    ModelState.AddModelError("ImageFile", "Chỉ nhận file .png");
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
                    product.Pic = "images/" + fileName;
                }
            }
            else
            {
                // factory apply default
                ProductFactory.ApplyDefaults(product);
            }

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(AdminIndex));
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Description,Price,CategoryId,Pic,IsFeatured")] Product product, IFormFile? ImageFile)
        {
            if (id != product.ProductId) return NotFound();

            ModelState.Remove("ImageFile");

            if (ModelState.IsValid)
            {
                try
                {
                    // image handling kept
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var ext = Path.GetExtension(ImageFile.FileName).ToLower();
                        if (ext != ".png")
                        {
                            ModelState.AddModelError("ImageFile", "Chỉ nhận file .png");
                            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
                            return View(product);
                        }

                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                        if (!string.IsNullOrEmpty(product.Pic) && !product.Pic.Contains("PlaceHolder.png"))
                        {
                            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), product.Pic);
                            if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                        }

                        var fileName = Guid.NewGuid().ToString() + ext;
                        var newPath = Path.Combine(uploadPath, fileName);
                        using (var stream = new FileStream(newPath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }
                        product.Pic = "images/" + fileName;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.ProductId == product.ProductId)) return NotFound();
                    else throw;
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
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null) return NotFound();

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
                if (!string.IsNullOrEmpty(product.Pic) && !product.Pic.Contains("PlaceHolder.png"))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/", product.Pic);
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }

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
