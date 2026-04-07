namespace MTKPM_Clothing_Store_web.Services;
using MTKPM_Clothing_Store_web.Models;
using System.Linq;

public class PriceAscStrategy : IProductSortStrategy
{
    public IQueryable<Product> Apply(IQueryable<Product> query) => query.OrderBy(p => p.Price);
}

public class PriceDescStrategy : IProductSortStrategy
{
    public IQueryable<Product> Apply(IQueryable<Product> query) => query.OrderByDescending(p => p.Price);
}

public class NameAscStrategy : IProductSortStrategy
{
    public IQueryable<Product> Apply(IQueryable<Product> query) => query.OrderBy(p => p.Name);
}

public class NameDescStrategy : IProductSortStrategy
{
    public IQueryable<Product> Apply(IQueryable<Product> query) => query.OrderByDescending(p => p.Name);
}

public class DefaultNameStrategy : IProductSortStrategy
{
    public IQueryable<Product> Apply(IQueryable<Product> query) => query.OrderBy(p => p.Name);
}