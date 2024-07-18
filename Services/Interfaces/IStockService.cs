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
        Task<List<StockTechInfoModel>> GetJumpEmptyStocks(DateTime selectedDate);
        Task<List<StockTechInfoModel>> GetBullishPullbackStocks(DateTime selectedDate);
        Task<List<StockTechInfoModel>> GetOrganizedStocks(DateTime selectedDate);
        Task<List<StockTechInfoModel>> GetSandwichStocks(DateTime selectedDate);
        Task<List<StockFinanceInfoModel>> GetFinanceIncreasingStocks();
        Task<List<StockBaseModel>> GetHighYieldStocks();
    }
}
