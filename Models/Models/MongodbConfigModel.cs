using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class MongodbConfigModel
    {
        public string? Name { get; set; }
        public List<EnumCollection>? Collections { get; set; }
    }
    public enum EnumCollection
    {
        Tech,
        EPS,
        Revenue,
        Dividend
    }
}
