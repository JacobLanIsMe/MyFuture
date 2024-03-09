using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class StockFinanceInfoModel : StockBaseModel
    {
        public List<StockEpsModel>? StockEpss { get; set; }
        public List<StockRevenueModel>? StockRevenues { get; set; }
    }
}
