using System;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.Models;

public partial class VwRecentOrder
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public DateTime? OrderDate { get; set; }
}
