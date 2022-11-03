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

        protected static HttpClient Client;
        protected static bool IsClientSetUp = false;
        protected static SocketIO Socket;
        protected static bool IsSocketSetUp = false;

        protected string _bearerToken;
        protected string _baseUrl;
        protected string _token;

        private BrokerConfig _config;
        private ILogger _logger;

        static AutoTradeController() 
        {
            Program.ApplicationShuttingDown += Program_ApplicationShuttingDown;
            if (!IsClientSetUp) SetUpClient();
        }

        public AutoTradeController(IConfiguration config, ILogger<AutoTradeController> logger)
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

            this._baseUrl = this._config.BaseUrl;
            this._token = this._config.Token;
            if (!IsClientSetUp) SetUpClient();
            if (!IsSocketSetUp) SocketSetUp(this._baseUrl, this._token, this);
        }

        [HttpGet("ping")]
        public async Task<ActionResult> Ping()
        {
            long ms = 0;
            while (!AutoTradeController.IsSocketSetUp && ms < SOCKET_CONNECTION_TRESHOLD)
            {
                await Task.Delay(250);
                ms += 250;
            }
            return new OkObjectResult($"ConnectionTime: {ms}ms; SocketId: {Socket.Id}");
        }

        [HttpPost("trade")]
        public async Task<ActionResult> Trade([FromBody] TradersViewRequest data)
        {
            var requestBody = new BrokersRequest(this._config.AccountId.ToString(),
                                                        this._config.OrderType,
                                                        this._config.TimeInForce,
                                                        data);

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{this._baseUrl}{openTradePath}"),
                Content = new FormUrlEncodedContent(requestBody.ToKeyValuePairs())
            };
            TradeOperations.SetHeaders(Client);
            HttpResponseMessage? message = await Client.SendAsync(request);

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
            TradeOperations.SetHeaders(Client);
            HttpResponseMessage? message = await Client.SendAsync(request);
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
        }

        private static async void SocketClose()
        {
            await Socket.DisconnectAsync();
            Console.WriteLine("Socket closed.");
        }

        private void OnSocketErrored(object? sender, string e)
        {
            Console.WriteLine($"Socket Errored: {sender} ;{e}");
            this._logger.LogError($"Socket Errored: {sender} ;{e}");
        }

        private void OnSocketConnected(object? sender, EventArgs e)
        {
            this._bearerToken = $"{Socket.Id}{this._token}";
            TradeOperations.SetAuthHeader(Client, this._bearerToken);
            Console.WriteLine($"connected: {Socket.Connected}.");
            Console.WriteLine($"token: {this._bearerToken}.");
            this._logger.LogInformation($"connected: {Socket.Connected}.");
            this._logger.LogInformation($"token: {this._bearerToken}.");
            IsSocketSetUp = true;
        }

        private static void Program_ApplicationShuttingDown()
        {
            SocketClose();
        }
    }
}
