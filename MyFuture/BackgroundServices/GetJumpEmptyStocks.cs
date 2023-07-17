using Microsoft.Extensions.Caching.Memory;

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
            await Process();   
        }
        private async Task Process()
        {
            HttpClient client = new HttpClient();
            string url = "https://tw.quote.finance.yahoo.net/quote/q?type=ta&perd=d&mkt=10&sym=2330&v=1&callback=jQuery111306311117094962886_1574862886629&_=1574862886630";
            var responseMsg = await client.GetAsync(url);
            string? result = null;
            string cacheKey = $"{DateTime.Now.Date}_2330"; 
            if (responseMsg.IsSuccessStatusCode)
            {
                result = await responseMsg.Content.ReadAsStringAsync();
            }
            _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(1));
        }

    }
}
