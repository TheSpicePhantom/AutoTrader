using AutoTrader.Helpers;
using AutoTrader.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrader.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AutoTradeController
    {
        private const int SOCKET_CONNECTION_TRESHOLD = 20000; //ms
        private SocketOperator _operator;

        public AutoTradeController(SocketOperator socketOperator)
        {
            this._operator = socketOperator;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            long ms = 0;
            while (!this._operator.IsSocketSetUp && ms < SOCKET_CONNECTION_TRESHOLD)
            {
                await Task.Delay(250);
                ms += 250;
            }
            return new OkObjectResult($"ConnectionTime: {ms}ms; SocketId: {this._operator.Socket.Id}");
        }

        [HttpPost("trade-open")]
        public async Task<IActionResult> Trade([FromBody] TradersViewBuySellRequest data)
        {
            await Ping();
            return new OkObjectResult(await this._operator.OpenTrade(data));
        }

        [HttpPost("trade-close")]
        public async Task<IActionResult> TradeClose([FromBody] CloseTradeRequest data)
        {
            await Ping();
            return new OkObjectResult(await this._operator.CloseTrade(data));
        }

        [HttpPost("trade-multiclose")]
        public async Task<IActionResult> TradeClose([FromBody] CloseTradeMultipleRequest data)
        {
            await Ping();
            return new OkObjectResult(await this._operator.CloseTrade(data));
        }

        [HttpGet("close-all-symbol")]
        public async Task<IActionResult> CloseAllForSymbol(string symbol)
        {
            await Ping();
            return new OkObjectResult(await this._operator.CloseAllForSymbol(symbol));
        }

        [HttpPost("close-all-symbol")]
        public async Task<IActionResult> CloseAllForSymbolPost([FromBody] string symbol)
        {
            await Ping();
            return new OkObjectResult(await this._operator.CloseAllForSymbol(symbol));
        }

        [HttpPost("trade-hedge")]
        public async Task<IActionResult> TradeHedge([FromBody] TradersViewBuySellRequest data)
        {
            await Ping();
            return new OkObjectResult(await this._operator.OpenHedgeTransaction(data));
        }

        [HttpGet("trade-list-active")]
        public IActionResult TradeListActive()
        {
            return new OkObjectResult(this._operator.Transactions);
        }

        [HttpGet("getInstruments")]
        public async Task<object> GetInstruments()
        {
            await Ping();
            return new OkObjectResult(await this._operator.GetInstruments());
        }
    }
}
