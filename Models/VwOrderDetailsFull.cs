using System;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.Models;

public partial class VwOrderDetailsFull
{
    public int DetailId { get; set; }

    public int? OrderId { get; set; }

    public int? ProductId { get; set; }

    public int Quantity { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }
}
