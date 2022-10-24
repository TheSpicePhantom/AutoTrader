using AutoTrader.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrader.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class AutoTradeController : Controller
    {

        private BrokerConfig _config;
        private string error = "";

        public AutoTradeController(IConfiguration config)
        {
            this.error = $"configin: {config.ToString()}\n";
            try
            {
                this._config = new BrokerConfig(config.GetSection("BrokerConfig"));
                this.error += $"configout: {this._config.ToString()}";
            }
            catch(Exception ex)
            {
                this.error += $"Error occured: {ex}";
            }

        }

        [HttpPost("trade")]
        public async Task<object> Trade([FromBody] TradersViewRequest data)
        {
            //HttpClient client = new HttpClient();
            return new OkObjectResult(this.error);
        }
    }
}
