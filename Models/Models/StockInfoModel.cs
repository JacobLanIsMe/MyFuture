using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class StockInfoModel
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public List<StockDetailModel>? StockDetails { get; set; }
    }
}
