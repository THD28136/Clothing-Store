using System;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.Models;

public partial class OrderDetail
{
    public int DetailId { get; set; }
    public int? OrderId { get; set; }
    public int? ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; } // Lưu giá tại thời điểm mua

    public virtual Order? Order { get; set; }
    public virtual Product? Product { get; set; }
}