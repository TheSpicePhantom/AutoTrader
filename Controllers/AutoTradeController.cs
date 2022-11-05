using AutoTrader.Helpers;
using AutoTrader.Models;
using Microsoft.AspNetCore.Mvc;
using SocketIOClient;

#pragma warning disable CS8618
namespace AutoTrader.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AutoTradeController
    {
        private const int SOCKET_CONNECTION_TRESHOLD = 20000; //ms
        private const string getInstrumentsPath = "/trading/get_instruments";
        private const string openTradePath = "/trading/open_trade";
        private const string closeTradePath = "/trading/close_trade";

        protected string _baseUrl;
        protected string _token;

        private BrokerConfig _config;
        private ILogger _logger;

        private SocketOperator _operator;


        public AutoTradeController(IConfiguration config, ILogger<AutoTradeController> logger, SocketOperator socketOperator)
        {
            this._logger = logger;
            try
            {
                this._config = new BrokerConfig(config.GetSection("BrokerConfig"));
            }
            catch(Exception ex)
            {
                this._logger.LogInformation($"Error occured: {ex}");
                throw new ApplicationException("No config present!");
            }

            this._operator = socketOperator;
            this._baseUrl = this._config.BaseUrl;
            this._token = this._config.Token;
        }

        [HttpGet("ping")]
        // check when socket is closed and if we can run it on schedule
        // add socket on disconnect
        public async Task<ActionResult> Ping()
        {
            long ms = 0;
            while (!this._operator.IsSocketSetUp && ms < SOCKET_CONNECTION_TRESHOLD)
            {
                await Task.Delay(250);
                ms += 250;
            }
            return new OkObjectResult($"ConnectionTime: {ms}ms; SocketId: {this._operator.Socket.Id}");
        }

        [HttpPost("trade")]
        public async Task<ActionResult> Trade([FromBody] TradersViewBuySellRequest data)
        {
            var requestBody = new BrokerOpenTradeMessage(this._config.AccountId.ToString(),
                                                        this._config.OrderType,
                                                        this._config.TimeInForce,
                                                        data);

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{this._baseUrl}{openTradePath}"),
                Content = new FormUrlEncodedContent(requestBody.ToKeyValuePairs())
            };
            TradeOperations.SetHeaders(this._operator.Client);
            HttpResponseMessage? message = await this._operator.Client.SendAsync(request);

            string apiResponse;

            if (message == null || !message.IsSuccessStatusCode)
            {
                apiResponse = $"response: {(message != null ? message.ToString() : "message is null")}";
                this._logger.LogInformation(apiResponse);
            }
            else
            {
                apiResponse = await message.Content.ReadAsStringAsync();
                this._logger.LogInformation(apiResponse);
            }
            return new OkObjectResult(apiResponse);
        }

        [HttpGet("closeTrade")]
        public async Task<IActionResult> CloseTrade([FromBody] TradersViewBuySellRequest data)
        {
            var requestBody = new BrokerCloseTradeMessage(this._config.AccountId.ToString(),
                                                        this._config.OrderType,
                                                        this._config.TimeInForce,
                                                        null,null, 
                                                        data);

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{this._baseUrl}{closeTradePath}"),
                Content = new FormUrlEncodedContent(requestBody.ToKeyValuePairs())
            };

            TradeOperations.SetHeaders(this._operator.Client);
            HttpResponseMessage? message = await this._operator.Client.SendAsync(request);

            string apiResponse;

            if (message == null || !message.IsSuccessStatusCode)
            {
                apiResponse = $"response: {(message != null ? message.ToString() : "message is null")}";
                this._logger.LogInformation(apiResponse);
            }
            else
            {
                apiResponse = await message.Content.ReadAsStringAsync();
                this._logger.LogInformation(apiResponse);
            }
            return new OkObjectResult(apiResponse);
        }

        [HttpGet("getInstruments")]
        public async Task<object> GetInstruments()
        {
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{this._baseUrl}{getInstrumentsPath}"),
            };
            TradeOperations.SetHeaders(this._operator.Client);
            HttpResponseMessage? message = await this._operator.Client.SendAsync(request);
            string apiResponse;

            if (message == null || !message.IsSuccessStatusCode)
            {
                apiResponse = $"response: {(message != null ? message.ToString() : "message is null")}";
                this._logger.LogInformation(apiResponse);
            }
            else
            {
                apiResponse = await message.Content.ReadAsStringAsync();
                this._logger.LogInformation(apiResponse);
            }
            return new OkObjectResult(apiResponse);
        }
    }
}
