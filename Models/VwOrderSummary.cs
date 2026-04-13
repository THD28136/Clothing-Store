using System;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.Models;

public partial class VwOrderSummary
{
    public int OrderId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? OrderDate { get; set; }
}
