using System;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.Models;

public partial class VwCategoryProductCount
{
    public string Name { get; set; } = null!;

    public int? TotalProducts { get; set; }
}
