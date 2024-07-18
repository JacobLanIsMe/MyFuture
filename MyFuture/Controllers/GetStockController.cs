using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.Models;
using Services.Interfaces;

namespace MyFuture.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetStockController : ControllerBase
    {
        private readonly IStockService _stockService;
        public GetStockController(IStockService stockService)
        {
            _stockService = stockService;
        }
        [HttpGet("GetJumpEmptyStocks")]
        public async Task<ApiDataResponseModel> GetJumpEmptyStocks(DateTime selectedDate)
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = await _stockService.GetJumpEmptyStocks(selectedDate);
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
        [HttpGet("GetBullishPullbackStocks")]
        public async Task<ApiDataResponseModel> GetBullishPullbackStocks(DateTime selectedDate)
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = await _stockService.GetBullishPullbackStocks(selectedDate);
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
        [HttpGet("GetOrganizedStocks")]
        public async Task<ApiDataResponseModel> GetOrganizedStocks(DateTime selectedDate)
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = await _stockService.GetOrganizedStocks(selectedDate);
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
        [HttpGet("GetSandwichStocks")]
        public async Task<ApiDataResponseModel> GetSandwichStocks(DateTime selectedDate)
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = await _stockService.GetSandwichStocks(selectedDate);
                result.SetSuccess();
            }
            catch(Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
        [HttpGet("GetFinanceIncreasingStocks")]
        public async Task<ApiDataResponseModel> GetFinanceIncreasingStocks()
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = await _stockService.GetFinanceIncreasingStocks();
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
        [HttpGet("GetHighYieldStocks")]
        public async Task<ApiDataResponseModel> GetHighYieldStocks()
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
                result.Data = await _stockService.GetHighYieldStocks();
                result.SetSuccess();
            }
            catch (Exception ex)
            {
                result.SetError(ex.Message, ex.ToString());
            }
            return result;
        }
    }
}
