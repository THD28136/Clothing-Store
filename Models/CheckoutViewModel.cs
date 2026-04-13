using System.Collections.Generic;
using MTKPM_Clothing_Store_web.Models;

namespace MTKPM_Clothing_Store_web.Models
{
    public class CheckoutItem
    {
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
    }

    public class CheckoutViewModel
    {
        public List<CheckoutItem> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Product.Price * i.Quantity);
    }
}