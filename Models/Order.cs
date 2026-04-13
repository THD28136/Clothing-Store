using System;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.Models;

public partial class Order
{
    public int OrderId { get; set; }
    public int? UserId { get; set; }
    public DateTime? OrderDate { get; set; }

    public string Status { get; set; } = "Pending"; // Pending, Shipping, Completed, Cancelled

    public decimal TotalAmount { get; set; } // Tổng tiền

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public virtual User? User { get; set; }
}
