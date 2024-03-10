using Caches.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDbProvider;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caches.Caches
{
    public class CacheStockDividend : ICacheStockDividend
    {
        private readonly IStockRepository _stockRepository;
        private readonly IMongoDbService _mongoDbService;
        private readonly ILogger<CacheStockDividend> _logger;

        public CacheStockDividend(IStockRepository stockRepository, IMongoDbService mongoDbService, ILogger<CacheStockDividend> logger)
        {
            _stockRepository = stockRepository;
            _mongoDbService = mongoDbService;
            _logger = logger;
        }

        public async Task SetStockDividendCache()
        {
            List<string> stockIds = _stockRepository.GetStockIds(); // 取得所有的 stockId

        }
    }
}
