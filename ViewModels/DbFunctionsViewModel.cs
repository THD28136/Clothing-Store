using System;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.ViewModels
{
    public class DbFunctionsViewModel
    {
        public int? TotalOrders { get; set; }
        public int? TotalProducts { get; set; }
        public DateTime? ServerNow { get; set; }
        public decimal? TotalRevenue { get; set; }
        public int ProductCount { get; set; }
        public int InStockCount { get; set; }
        public int RecentOrdersCount { get; set; }
        public List<TopProduct> TopSelling { get; set; } = new();
        public string? Error { get; set; }
    }

    public class TopProduct
    {
        public string Name { get; set; } = string.Empty;
        public int TotalSold { get; set; }
    }
}