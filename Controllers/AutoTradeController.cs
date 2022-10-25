using AutoTrader.Helpers;
using AutoTrader.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrader.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class AutoTradeController : SocketUsingControllerBase
    {
        private const string getInstrumentsPath = "/trading/get_instruments";

        public class InlineLogger : ILogger
        {
            public string message = "";
            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                throw new NotImplementedException();
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                this.message += $"{state}\n";
            }

            public void LogInformation(string? message, params object?[] args)
            {
                this.message += $"{message}\n";
            }
        }

        private BrokerConfig _config;
        private ILogger _logger;

        public AutoTradeController(IConfiguration config) : base(config)
        {
            this._logger = new InlineLogger();
            try
            {
                this._config = new BrokerConfig(config.GetSection("BrokerConfig"));
            }
            catch(Exception ex)
            {
                this._logger.LogInformation($"Error occured: {ex}");
                throw new ApplicationException("No config present!");
            }

            base._baseUrl = this._config.BaseUrl;
            base._token = this._config.Token;
        }

        [HttpPost("trade")]
        public async Task<object> Trade([FromBody] TradersViewRequest data)
        {
            //this._logger.LogInformation(await this._operations.GetInstruments());
            return new OkObjectResult(((InlineLogger)this._logger).message);
        }

        [HttpGet("getInstruments")]
        public async Task<object> GetInstruments()
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{this._baseUrl}{getInstrumentsPath}"),
            };
            TradeOperations.SetHeaders(ref request, this._bearerToken);
            var message = await this._httpClient.SendAsync(request);
            if (message == null || !message.IsSuccessStatusCode)
            {
                this._logger.LogInformation($"response: {message}");
            }
            else
            {
                return await message.Content.ReadAsStringAsync();
            }
            return "";
        }
    }
}
