﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class StockRevenueModel
    {
        public string? Month { get; set; }
        public double Revenue { get; set; }
        public double Mom { get; set; }
        public double LastYearRevenue { get; set; }
        public double Yoy { get; set; }
    }
}
