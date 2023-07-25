using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class StockFinanceDetailModel
    {
        public string? quarter { get; set; }
        public double eps { get; set; }
        public double qoq { get; set; }
        public double yoy { get; set; }
    }
}
