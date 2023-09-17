using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Models.Models
{
    public class StockBaseModel
    {
        public ObjectId Id { get; set; }
        public string? StockId { get; set; }
        public string? Name { get; set; }
    }
}
