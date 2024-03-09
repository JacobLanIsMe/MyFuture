using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models.Models;
using MongoDB.Driver;
using MongoDbProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetStockInfo.BackgroundServices
{
    public class WriteStockInfoToMemory : BackgroundService
    {
        private readonly ILogger<WriteStockInfoToMemory> _logger;
        private readonly IMongoDbService _mongoDbService;
        private IMongoClient _mongoClient;
        private readonly IMemoryCache _memoryCache;
        public WriteStockInfoToMemory(ILogger<WriteStockInfoToMemory> logger, IMongoDbService mongoDbService, IMemoryCache memoryCache)
        {
            _logger = logger;
            _mongoDbService = mongoDbService;
            _mongoClient = _mongoDbService.GetMongoClient();
            _memoryCache = memoryCache;
        } 
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Writing tech to Memory started");
                    var techCollection = _mongoClient.GetDatabase("MyFuture").GetCollection<StockTechInfoModel>("Tech");
                    var tech = await _mongoDbService.GetAllData<StockTechInfoModel>(techCollection);
                    _memoryCache.Set("Tech", tech, TimeSpan.FromMinutes(30));
                    _logger.LogInformation("Writing tech to Memory completed");
                    _logger.LogInformation("Writing eps to Memory started");
                    var epsCollection = _mongoClient.GetDatabase("MyFuture").GetCollection<StockEpsModel>("EPS");
                    var eps = await _mongoDbService.GetAllData<StockEpsModel>(epsCollection);
                    _memoryCache.Set("EPS", eps, TimeSpan.FromMinutes(30));
                    _logger.LogInformation("Writing eps to Memory completed");
                    _logger.LogInformation("Writing revenue to Memory started");
                    var revenueCollection = _mongoClient.GetDatabase("MyFuture").GetCollection<StockRevenueModel>("Revenue");
                    var revenue = await _mongoDbService.GetAllData<StockRevenueModel>(revenueCollection);
                    _memoryCache.Set("Revenue", revenue, TimeSpan.FromMinutes(30));
                    _logger.LogInformation("Writing revenue to Memory completed");
                }
                catch(Exception ex)
                {
                
                }
                await Task.Delay(TimeSpan.FromMinutes(10));
            }
        }
    }
}
