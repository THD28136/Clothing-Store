using System;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.Models;

public partial class VwFeaturedProduct
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string? Pic { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int? CategoryId { get; set; }

    public int Stock { get; set; }

    public bool IsFeatured { get; set; }
}
