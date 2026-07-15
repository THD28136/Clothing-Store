namespace MTKPM_Clothing_Store_web.Services;
using MTKPM_Clothing_Store_web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;

    /// <summary>
    /// Product service interface for business logic
    /// </summary>
    public interface IProductService
    {
        Task<Product?> GetProductByIdAsync(int productId);
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> GetVisibleProductsAsync();
        Task<IEnumerable<Product>> GetFeaturedProductsAsync();
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<List<Product>> GetProductsAsync(int? categoryId, decimal? minPrice, decimal? maxPrice, string? sort, string? q);
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
        Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10);
        Task<Product> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int productId);
        Task<bool> ToggleFeaturedAsync(int productId);
        Task<bool> UpdateStockAsync(int productId, int quantity);
    }

    /// <summary>
    /// Facade for product queries (filtering, searching, sorting).
    /// Controller can call GetProducts to obtain final list.
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetVisibleProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Stock > 0)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsFeatured)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId && p.Stock > 0)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<List<Product>> GetProductsAsync(int? categoryId, decimal? minPrice, decimal? maxPrice, string? sort, string? q)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(keyword) || 
                                        p.Description != null && p.Description.ToLower().Contains(keyword));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Filter visible products (either in stock or featured)
            query = query.Where(p => p.Stock > 0 || p.IsFeatured);

            var strategy = ProductSortContext.GetStrategy(sort);
            query = strategy.Apply(query);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetVisibleProductsAsync();

            var term = searchTerm.ToLower();
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => (p.Stock > 0 || p.IsFeatured) && 
                           (p.Name.ToLower().Contains(term) || 
                            p.Description != null && p.Description.ToLower().Contains(term)))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Stock > 0 && p.Price >= minPrice && p.Price <= maxPrice)
                .OrderBy(p => p.Price)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10)
        {
            return await _context.Products
                .Where(p => p.Stock > 0 && p.Stock <= threshold)
                .OrderBy(p => p.Stock)
                .ToListAsync();
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ArgumentException("Product name is required");

            if (product.Price < 0)
                throw new ArgumentException("Product price cannot be negative");

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Product {product.ProductId} created: {product.Name}");
            return product;
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            var existing = await _context.Products.FindAsync(product.ProductId);
            if (existing == null) return false;

            existing.Name = product.Name ?? existing.Name;
            existing.Description = product.Description ?? existing.Description;
            existing.Price = product.Price > 0 ? product.Price : existing.Price;
            existing.CategoryId = product.CategoryId ?? existing.CategoryId;
            existing.Image = product.Image ?? existing.Image;

            _context.Products.Update(existing);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Product {product.ProductId} updated");
            return true;
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Product {productId} deleted");
            return true;
        }

        public async Task<bool> ToggleFeaturedAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            product.IsFeatured = !product.IsFeatured;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Product {productId} featured status toggled to {product.IsFeatured}");
            return true;
        }

        public async Task<bool> UpdateStockAsync(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            product.Stock += quantity;
            if (product.Stock < 0) product.Stock = 0;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Product {productId} stock updated by {quantity}. New stock: {product.Stock}");
            return true;
        }
    }