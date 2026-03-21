using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;

namespace MTKPM_Clothing_Store_web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ClothingStoreContext _context;

        public ProductsController(ClothingStoreContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var clothingStoreContext = _context.Products.Include(p => p.Category);
            return View(await clothingStoreContext.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            // SelectList( [Nguồn dữ liệu], [Giá trị], [Tên hiển thị trên màn hình] );
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name"); 
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,Name,Description,Price,CategoryId")] Product product, IFormFile? ImageFile)
        {
            // Loại bỏ kiểm tra bắt buộc cho trường Pic trong Model
            ModelState.Remove("Pic");

            if (ModelState.IsValid)
            {
                // 1. Xử lý logic Ảnh
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    //Lấy extension của file
                    var ext = Path.GetExtension(ImageFile.FileName).ToLower();

                    // Kiểm tra định dạng
                    if (ext != ".png")
                    {
                        // Nếu không phải .png, thêm lỗi vào ModelState
                        ModelState.AddModelError("ImageFile", "Chỉ nhận file .png");
                    }
                    else
                    {
                        // Lưu file vào thư mục wwwroot/images
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

                        // Nếu thư mục chưa tồn tại thì tạo mới
                        if (!Directory.Exists(uploadPath)) 
                            Directory.CreateDirectory(uploadPath);

                        // Tạo tên file ngẫu nhiên để tránh trùng lặp
                        var fileName = Guid.NewGuid().ToString() + ext;
                        var path = Path.Combine(uploadPath, fileName);

                        // Lưu file vào đường dẫn đã xác định
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }
                        product.Pic = "images/" + fileName;
                    }
                }
                else
                {
                    // Không chọn ảnh thì dùng mặc định
                    product.Pic = "images/PlaceHolder.png";
                }

                // 2. Chỉ lưu khi ModelState vẫn còn Valid (sau khi đã kiểm tra file ở trên)
                if (ModelState.IsValid)
                {
                    _context.Add(product);
                    await _context.SaveChangesAsync();// Lưu vào CSDL
                    return RedirectToAction(nameof(Index));// Quay về trang danh sách sau khi tạo thành công
                }
            }

            // Nếu có bất kỳ lỗi nào (Dữ liệu text hoặc File ảnh), quay lại View
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);// Giữ lại giá trị đã chọn của CategoryId khi quay lại View
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Description,Price,CategoryId,Pic")] Product product, IFormFile? ImageFile)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            // Tách biệt việc kiểm tra hợp lệ của chuỗi Pic (đường dẫn cũ)
            ModelState.Remove("ImageFile");

            if (ModelState.IsValid)
            {
                try
                {
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Lấy extension của file
                        var ext = Path.GetExtension(ImageFile.FileName).ToLower();

                        // 1. Kiểm tra định dạng (giữ nguyên quy tắc chỉ nhận .png)
                        if (ext != ".png")
                        {
                            ModelState.AddModelError("ImageFile", "Chỉ nhận file .png");
                            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
                            return View(product);
                        }

                        // 2. Chuẩn bị đường dẫn
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                        // 3. Xóa ảnh cũ nếu người dùng upload ảnh mới
                        // Không xóa nếu ảnh cũ là ảnh mặc định (PlaceHolder.png)
                        if (!string.IsNullOrEmpty(product.Pic) && !product.Pic.Contains("PlaceHolder.png"))
                        {
                            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), product.Pic);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        // 4. Lưu ảnh mới
                        var fileName = Guid.NewGuid().ToString() + ext;
                        var newPath = Path.Combine(uploadPath, fileName);
                        using (var stream = new FileStream(newPath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }

                        // Cập nhật đường dẫn mới vào Model
                        product.Pic = "images/" + fileName;
                    }

                    // 5. Cập nhật vào Database
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.ProductId == product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
