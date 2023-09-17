﻿using Models.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Caches.Interfaces;
using Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Caches.Caches
{
    public class CacheStockTech : ICacheStockTech
    {
        // private readonly IMemoryCache _memoryCache;
        private readonly IStockRepository _stockRepository;
        private string? mongoConn = null;
        public CacheStockTech(/* IMemoryCache memoryCache,  */IStockRepository stockRepository, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            // _memoryCache = memoryCache;
            _stockRepository = stockRepository;
            mongoConn = config.GetConnectionString("Mongo");
        }
        public async Task SetStockTechCache()
        {
            MongoClient mongoClient = new MongoClient();
            mongoClient.GetDatabase("MyFuture");
            List<string> stockIds = _stockRepository.GetStockIds(); // 取得所有的 stockId
            #region 取得所有的 StockInfo，並寫入 Cache
            foreach (var stockId in stockIds)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    string url = $"https://tw.quote.finance.yahoo.net/quote/q?type=ta&perd=d&mkt=10&sym={stockId}&v=1&callback=jQuery111306311117094962886_1574862886629&_=1574862886630";
                    var responseMsg = await client.GetAsync(url);
                    string? detail = null;
                    string? name = null;
                    StockTechInfoModel stock = new StockTechInfoModel();
                    List<StockTechDetailModel> stockDetails = new List<StockTechDetailModel>();

                    if (responseMsg.IsSuccessStatusCode)
                    {
                        var data = await responseMsg.Content.ReadAsStringAsync();
                        detail = data.Split("\"ta\":")[1].Split(",\"ex\"")[0];
                        name = data.Split("\"name\":\"")[1].Split('\"')[0];
                    }
                    stockDetails = JsonSerializer.Deserialize<List<StockTechDetailModel>>(detail);
                    stock.StockDetails = stockDetails;
                    stock.Name = name;
                    stock.StockId = stockId;
                    if (!string.IsNullOrEmpty(name))
                    {
                        // _memoryCache.Set($"Tech{stockId}", stock);
                    }
                }
                catch (Exception ex)
                {

                }
            }
            #endregion
        }
    }
}
