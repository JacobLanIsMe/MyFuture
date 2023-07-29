using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class StockEpsModel
    {
        public string? Quarter { get; set; }
        public double Eps { get; set; }
        public double Qoq { get; set; }
        public double Yoy { get; set; }
    }
}
