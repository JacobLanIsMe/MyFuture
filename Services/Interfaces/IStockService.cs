using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IStockService
    {
        Task<List<StockTechInfoModel>> GetJumpEmptyStocks();
        Task<List<StockTechInfoModel>> GetBullishPullbackStocks();
        Task<List<StockTechInfoModel>> GetOrganizedStocks();
        Task<List<StockFinanceInfoModel>> GetFinanceIncreasingStocks();
        Task<List<StockBaseModel>> GetHighYieldStocks();
    }
}
