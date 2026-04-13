using System;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.Models;

public partial class VwTopSellingProduct
{
    public string Name { get; set; } = null!;

    public int? TotalSold { get; set; }
}
