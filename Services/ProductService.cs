namespace MTKPM_Clothing_Store_web.Services;
using MTKPM_Clothing_Store_web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Facade for product queries (filtering, searching, sorting).
/// Controller can call GetProducts to obtain final list.
/// </summary>
public class ProductService
{
    private readonly ClothingStoreContext _context;

    public ProductService(ClothingStoreContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetProductsAsync(int? categoryId, decimal? minPrice, decimal? maxPrice, string? sort, string? q)
    {
        var query = _context.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(p => p.Name.Contains(keyword));
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var strategy = ProductSortContext.GetStrategy(sort);
        query = strategy.Apply(query);

        return await query.ToListAsync();
    }
}