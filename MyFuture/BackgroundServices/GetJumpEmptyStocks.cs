using Microsoft.Extensions.Caching.Memory;
using MyFuture.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MyFuture.BackgroundServices
{
    public class GetJumpEmptyStocks : BackgroundService
    {
        private readonly IMemoryCache _memoryCache;
        public GetJumpEmptyStocks(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                List<string> stockList = GetStockList();
                if (!_memoryCache.TryGetValue($"{DateTime.Now.Date}_2330", out string value))
                {
                    await SetStockInfoCache();
                }
            }
            catch (Exception ex)
            {

            }
            
           
            //await Task.Delay(1000 * 60);
        }
        private async Task<List<StockDetailModel>> SetStockInfoCache()
        {
            HttpClient client = new HttpClient();
            string url = "https://tw.quote.finance.yahoo.net/quote/q?type=ta&perd=d&mkt=10&sym=2330&v=1&callback=jQuery111306311117094962886_1574862886629&_=1574862886630";
            var responseMsg = await client.GetAsync(url);
            string? data = null;
            List<StockDetailModel> result = new List<StockDetailModel>();
            string cacheKey = $"{DateTime.Now.Date}_2330"; 
            if (responseMsg.IsSuccessStatusCode)
            {
                data = await responseMsg.Content.ReadAsStringAsync();
                data = data.Split("\"ta\":")[1].Split(",\"ex\"")[0];
            }
            result = JsonSerializer.Deserialize<List<StockDetailModel>>(data);
            _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(1));
            return result;
        }
        private List<string> GetStockList()
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
