namespace MTKPM_Clothing_Store_web.Adapters;
using MTKPM_Clothing_Store_web.Models;

/// <summary>
/// Adapter: convert Product -> SessionCartItem for session cart usage.
/// Keeps conversion logic in one place and avoids colliding with the EF CartItem entity.
/// </summary>
public class ProductToCartItemAdapter
{
    private readonly Product _product;
    private readonly int _quantity;

    public ProductToCartItemAdapter(Product product, int quantity = 1)
    {
        _product = product;
        _quantity = quantity < 1 ? 1 : quantity;
    }

    public SessionCartItem Adapt()
    {
        return new SessionCartItem
        {
            ProductId = _product.ProductId,
            Name = _product.Name,
            Price = _product.Price,
            Quantity = _quantity,
            Pic = _product.Image
        };
    }
}