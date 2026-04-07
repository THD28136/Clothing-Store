namespace MTKPM_Clothing_Store_web.Services;
using MTKPM_Clothing_Store_web.Models;

/// <summary>
/// Small factory for Product creation helpers (set defaults).
/// </summary>
public static class ProductFactory
{
    public static void ApplyDefaults(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Pic))
        {
            product.Pic = "images/PlaceHolder.png";
        }
    }
}