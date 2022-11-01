using AutoTrader.Helpers;
using AutoTrader.Models;
using Microsoft.AspNetCore.Mvc;
using SocketIOClient;

namespace AutoTrader.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class AutoTradeController
    {
        private const string getInstrumentsPath = "/trading/get_instruments";
        private const string openTradePath = "/trading/open_trade";

#pragma warning disable CS8618
        protected static HttpClient Client;
        protected static bool IsClientSetUp = false;
        protected static SocketIO Socket;
        protected static bool IsSocketSetUp = false;
#pragma warning restore CS8618

        protected string _bearerToken;
        protected string _baseUrl;
        protected string _token;

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

#pragma warning disable CS8618

        static AutoTradeController()
        {
            Program.ApplicationShuttingDown += Program_ApplicationShuttingDown;
            if (!IsClientSetUp) SetUpClient();
            if (!IsSocketSetUp) SocketSetUp(null, null, null);
        }

        public AutoTradeController(IConfiguration config)
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

            this._baseUrl = this._config.BaseUrl;
            this._token = this._config.Token;
            if (!IsClientSetUp) SetUpClient();
            if (!IsSocketSetUp) SocketSetUp(this._baseUrl, this._token, this);
        }
#pragma warning restore CS8618

        [HttpGet("ping")]
        public ActionResult Ping()
        {
            // TODO update here to return only AFTER socket initialized
            return new OkObjectResult("pong");
        }

        [HttpPost("trade")]
        public async Task<ActionResult> Trade([FromBody] TradersViewRequest data)
        {
            var requestBody = new BrokersRequest(this._config.AccountId.ToString(),
                                                        this._config.OrderType,
                                                        this._config.TimeInForce,
                                                        data);

            var converted = requestBody.ToKeyValuePairs();

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{this._baseUrl}{openTradePath}"),
                Content = new FormUrlEncodedContent(requestBody.ToKeyValuePairs())
            };
            TradeOperations.SetHeaders(Client);
            HttpResponseMessage? message = await Client.SendAsync(request);

            if (message == null || !message.IsSuccessStatusCode)
            {
                this._logger.LogInformation($"response: {message}");
            }
            else
            {
                this._logger.LogInformation($"{await message.Content.ReadAsStringAsync()}");
            }
            return new OkObjectResult(((InlineLogger)this._logger).message);
        }

        [HttpGet("getInstruments")]
        public async Task<object> GetInstruments()
        {
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{this._baseUrl}{getInstrumentsPath}"),
            };
            TradeOperations.SetHeaders(Client);
            HttpResponseMessage? message = await Client.SendAsync(request);
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



        private static void SetUpClient()
        {
            Client = new HttpClient();
            IsClientSetUp = true;
        }

        private static void SocketSetUp(string? baseUrl, string? token, AutoTradeController? _this)
        {
            if (baseUrl == null || token == null || _this == null) return;

            if (IsSocketSetUp) return;

            Socket = new SocketIO(baseUrl, new SocketIOOptions
            {
                Query = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("access_token", token)
                }
            });
            Socket.OnConnected += _this.OnSocketConnected;
            Socket.OnError += _this.OnSocketErrored;
            Socket.ConnectAsync().GetAwaiter().GetResult();
            IsSocketSetUp = true;
        }

        private static async void SocketClose()
        {
            await Socket.DisconnectAsync();
            Console.WriteLine("Socket closed.");
        }

        private void OnSocketErrored(object? sender, string e)
        {
            Console.WriteLine($"Socket Errored: {sender} ;{e}");
        }

        private void OnSocketConnected(object? sender, EventArgs e)
        {
            this._bearerToken = $"{Socket.Id}{this._token}";
            TradeOperations.SetAuthHeader(Client, this._bearerToken);
            Console.WriteLine($"connected: {Socket.Connected}.");
            Console.WriteLine($"token: {this._bearerToken}.");
        }

        private static void Program_ApplicationShuttingDown()
        {
            SocketClose();
        }
    }
}
