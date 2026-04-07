namespace MTKPM_Clothing_Store_web.Services;
using System;

public static class ProductSortContext
{
    public static IProductSortStrategy GetStrategy(string? sort)
    {
        return sort switch
        {
            "price_asc" => new PriceAscStrategy(),
            "price_desc" => new PriceDescStrategy(),
            "name_asc" => new NameAscStrategy(),
            "name_desc" => new NameDescStrategy(),
            _ => new DefaultNameStrategy()
        };
    }
}