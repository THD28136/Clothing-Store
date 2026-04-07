namespace MTKPM_Clothing_Store_web.Services;
using MTKPM_Clothing_Store_web.Models;
using System.Linq;

public interface IProductSortStrategy
{
    IQueryable<Product> Apply(IQueryable<Product> query);
}