using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyFuture.Interfaces;
using MyFuture.Models;

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
        public async Task<ApiDataResponseModel> GetJumpEmptyStocks()
        {
            ApiDataResponseModel result = new ApiDataResponseModel();
            try
            {
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
