using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
    public class Stock<T>
    {
        public List<T>? Data { get; set; }
    }
}
