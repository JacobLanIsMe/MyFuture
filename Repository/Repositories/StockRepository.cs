using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repositories
{
    public class StockRepository : IStockRepository
    {
        public List<string> GetStockIds()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "IPO&OTC.csv");
            if (!File.Exists(path))
            {
                throw new Exception("Cannot find IPO&OTC.csv");
            }
            var reader = new StreamReader(File.OpenRead(path));
            List<string> result = new List<string>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                foreach (var i in values)
                {
                    if (int.TryParse(i, out int a))
                    {
                        result.Add(i);
                    }
                }
            }
            return result;
        }
    }
}
