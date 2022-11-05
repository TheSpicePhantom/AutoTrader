using AutoTrader.Models;
using Newtonsoft.Json;
using SocketIOClient;
using System.Net.Http.Headers;

namespace AutoTrader.Helpers
{
    public sealed class SocketOperator
    {
        private enum ResponseType
        {
            AllCorrect = 200,
            AllFailed = 400,
            CreateCorrectCloseFailed = 300,
            CreateCorrectClosePartiallyFailed = 301,
            CreateFailedCloseCorrect = 302,
            CreateFailedClosePartiallyFailed = 303,
        }

        private const string GET_INSTRUMENTS_PATH = "/trading/get_instruments";
        private const string OPEN_TRADE_PATH = "/trading/open_trade";
        private const string CLOSE_TRADE_PATH = "/trading/close_trade";

        public bool IsRunning { get; private set; } = true;
        public bool IsClientSetUp { get; private set; } = false;
        public bool IsSocketSetUp { get; private set; } = false;
        public List<OpenTransaction> Transactions { get; private set; } = new();
        public HttpClient Client { get; private set; }
        public SocketIO Socket { get; private set; }
        public string BearerToken { get; private set; }
        public string Token { get; private set; }
        public string BaseUrl { get; private set; }
        public string AccountId  { get; private set; } 
        public string OrderType  { get; private set; } 
        public string TimeInForce  { get; private set; } 

        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        
#pragma warning disable CS8618
        public SocketOperator(IConfiguration config, ILogger<SocketOperator> logger)
        {
            Program.ApplicationShuttingDown += Program_ApplicationShuttingDown;

            this._logger = logger;
            this._config = config.GetSection("BrokerConfig");

            this.Token = this._config.GetValue<string>("token");
            this.BaseUrl = this._config.GetValue<string>("baseUrl");
            this.AccountId = this._config.GetValue<string>("accountId");
            this.OrderType = this._config.GetValue<string>("orderType");
            this.TimeInForce = this._config.GetValue<string>("timeInForce");

            SetUpClient();
            SocketSetUp();
        }
#pragma warning restore CS8618

        public async Task<OpenCloseTradeResponse> OpenHedgeTransaction(TradersViewBuySellRequest data)
        {
            string name = data._symbol.ticker;
            bool isBuy = data._is_buy == "buy";
            double amount = data._contracts.orders;

            OpenTransaction? transaction = GetTransaction(name);
            OpenCloseTradeResponse response;

            if (transaction == null)
            {
                response = await OpenTrade(data);
                if (!response.response.executed) return response;

                AddTransaction(name, isBuy, response.data.orderId, amount);
            }
            else
            {
                Dictionary<string, double> toClose = isBuy ? transaction.SellOrders : transaction.BuyOrders;
                Dictionary<string, bool> transactionsClosed = new();

                foreach (var t in toClose)
                {
                    var closeTradeResp = await CloseTrade(t.Key, t.Value);
                    transactionsClosed.Add(closeTradeResp.data.orderId, closeTradeResp.response.executed);
                }

                OpenCloseTradeResponse openTradeResp = await OpenTrade(data);
                string openTransactionId = "-1";

                if (openTradeResp.response.executed) 
                {
                    openTransactionId = openTradeResp.data.orderId;
                    AddTransaction(name, isBuy, openTransactionId, amount);
                }
                
                bool allCloseExecuted = transactionsClosed.All(x => x.Value);
                bool anyCloseExecuted = transactionsClosed.Any(x => x.Value);
                bool executed = openTradeResp.response.executed || allCloseExecuted;
                ResponseType responseCode = openTradeResp.response.executed
                    ? allCloseExecuted ? ResponseType.AllCorrect : anyCloseExecuted
                        ? ResponseType.CreateCorrectClosePartiallyFailed : ResponseType.CreateCorrectCloseFailed
                    : allCloseExecuted ? ResponseType.CreateFailedCloseCorrect : allCloseExecuted
                        ? ResponseType.CreateFailedClosePartiallyFailed : ResponseType.AllFailed;
                string ct = $"closedTransactions: {JsonConvert.SerializeObject(transactionsClosed)}";

                response = PrepareOpenCloseTradeResponse(executed, (int)responseCode, openTransactionId, ct);                
            }

            return response;
        }

        public async Task<OpenCloseTradeResponse> OpenTrade(TradersViewBuySellRequest data) 
        {
            BrokerOpenTradeMessage requestBody = new(this.AccountId.ToString(), this.OrderType,
                                                        this.TimeInForce, data);

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{this.BaseUrl}{OPEN_TRADE_PATH}"),
                Content = new FormUrlEncodedContent(requestBody.ToKeyValuePairs())
            };
            SetHeaders();
            HttpResponseMessage? message = await this.Client.SendAsync(request);

            return await HandleBrokersResponse(message);
        }

        public async Task<OpenCloseTradeResponse> CloseTrade(CloseTradeRequest data)
        {
            return await CloseTrade(data.tradeId, data.amount);
        }

        public async Task<OpenCloseTradeResponse> CloseTrade(CloseTradeMultipleRequest data)
        {
            Dictionary<string, bool> transactionsClosed = new();

            foreach (var t in data.tradeIdAmountPairs)
            {
                var closeTradeResp = await CloseTrade(t.Key, t.Value);
                transactionsClosed.Add(closeTradeResp.data.orderId, closeTradeResp.response.executed);
            }
            
            return PrepareOpenCloseTradeResponse(true, -2, "-2", $"closedTransactions: {JsonConvert.SerializeObject(transactionsClosed)}");
        }

        public async Task<OpenCloseTradeResponse> CloseTrade(string tradeId, double amount)
        {
            BrokerCloseTradeMessage requestBody = new(this.OrderType, this.TimeInForce, 
                                                        tradeId, null, null, amount);

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{this.BaseUrl}{CLOSE_TRADE_PATH}"),
                Content = new FormUrlEncodedContent(requestBody.ToKeyValuePairs())
            };

            SetHeaders();
            HttpResponseMessage? message = await this.Client.SendAsync(request);

            return await HandleBrokersResponse(message);
        }

        public async Task<OpenCloseTradeResponse> GetInstruments()
        {
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{this.BaseUrl}{GET_INSTRUMENTS_PATH}"),
            };
            SetHeaders();
            HttpResponseMessage? message = await this.Client.SendAsync(request);

            return await HandleBrokersResponse(message);
        }

        private async Task<OpenCloseTradeResponse> HandleBrokersResponse(HttpResponseMessage? message)
        {
            OpenCloseTradeResponse apiResponse;
            if (message == null || !message.IsSuccessStatusCode || message.Content == null)
            {
                apiResponse = PrepareErrorMessage(message);
                this._logger.LogInformation(JsonConvert.SerializeObject(apiResponse));
            }
            else
            {
                OpenCloseTradeResponse? deserializedTemp = JsonConvert.DeserializeObject<OpenCloseTradeResponse>(await message.Content.ReadAsStringAsync());
                apiResponse = deserializedTemp == null ? PrepareErrorMessage(message) : deserializedTemp;
                this._logger.LogInformation(JsonConvert.SerializeObject(apiResponse));
            }

            return apiResponse;
        }

        private OpenTransaction? GetTransaction(string name)
        {
            return this.Transactions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        private void AddTransaction(string name, bool isBuy, string orderId, double amount)
        {
            var existingTransaction = GetTransaction(name);
            if (existingTransaction == null) 
            {
                if (isBuy) this.Transactions.Add(new OpenTransaction(name, new() { { orderId, amount } }, new()));
                else this.Transactions.Add(new OpenTransaction(name, new(), new() { { orderId, amount } }));
            }
            else
            {
                if (isBuy) existingTransaction.BuyOrders.Add(orderId, amount );
                else existingTransaction.SellOrders.Add(orderId, amount);
            }
        }

        private OpenCloseTradeResponse PrepareErrorMessage(HttpResponseMessage? message)
        {
            return PrepareOpenCloseTradeResponse(false, -1, "-1", message != null ? message.ToString() : "message is null");
        }

        private OpenCloseTradeResponse PrepareOpenCloseTradeResponse(bool executed, int type, string transactionId, string message)
        {
            return new OpenCloseTradeResponse(new OCTR_Response(executed),
                    new OCTR_Data(type, transactionId, message));
        }

        private void SetUpClient()
        {
            this.Client = new HttpClient();
            this.IsClientSetUp = true;
        }

        private void SocketSetUp()
        {
            if (this.BaseUrl == null || this.Token == null) return;

            if (this.IsSocketSetUp) return;

            this.Socket = new SocketIO(this.BaseUrl, new SocketIOOptions
            {
                Query = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("access_token", this.Token)
                }
            });
            this.Socket.OnConnected += this.OnSocketConnected;
            this.Socket.OnError += this.OnSocketErrored;
            this.Socket.OnDisconnected += this.OnSocketDisconnected;
            this.Socket.ConnectAsync().GetAwaiter().GetResult();
        }

        private async void SocketClose()
        {
            await this.Socket.DisconnectAsync();
            Console.WriteLine("Socket closed.");
        }

        public void SetHeaders()
        {
            SetHeader("Accept", "application/json");
            SetHeader("User-Agent", "request");
        }

        public void SetAuthHeader()
        {
            if (this.Client.DefaultRequestHeaders.Contains("Authorization"))
            {
                this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.BearerToken);
            }
            else
            {
                this.Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {this.BearerToken}");
            }
        }

        public void SetHeader(string header, string value)
        {
            if (this.Client.DefaultRequestHeaders.Contains(header))
            {
                this.Client.DefaultRequestHeaders.Remove(header);
                this.Client.DefaultRequestHeaders.Add(header, value);
            }
            else
            {
                this.Client.DefaultRequestHeaders.Add(header, value);
            }
        }

        private void OnSocketErrored(object? sender, string e)
        {
            Console.WriteLine($"Socket Errored: {sender} ;{e}");
            this._logger.LogError($"Socket Errored: {sender} ;{e}");
        }

        private void OnSocketConnected(object? sender, EventArgs e)
        {
            this.BearerToken = $"{Socket.Id}{this.Token}";
            SetAuthHeader();
            Console.WriteLine($"connected: {this.Socket.Connected}.");
            Console.WriteLine($"token: {this.BearerToken}.");
            this._logger.LogInformation($"connected: {this.Socket.Connected}.");
            this._logger.LogInformation($"token: {this.BearerToken}.");
            this.IsSocketSetUp = true;
        }

        private void OnSocketDisconnected(object? sender, string e)
        {
            Console.WriteLine($"Socket Disconnected: {sender} ;{e}");
            this._logger.LogError($"Socket Disconnected: {sender} ;{e}");
            if (this.IsRunning) SocketSetUp();
        }

        private void Program_ApplicationShuttingDown()
        {
            this.IsRunning = false;
            SocketClose();
        }
    }
}
