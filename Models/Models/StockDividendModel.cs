using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class StockDividendModel : StockBaseModel
    {
        public List<StockDevidendDetailModel>? DividendList { get; set; }
    }
    public class StockDevidendDetailModel
    {
        public int Year { get; set; }
        public double CashDividend { get; set; }
        public double StockDividend { get; set; }
    }
}
