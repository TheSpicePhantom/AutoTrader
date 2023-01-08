using AutoTrader.Models;
using Newtonsoft.Json;
using SocketIOClient;
using System.Net.Http.Headers;

namespace AutoTrader.Helpers
{
    public sealed class SocketOperator : IDisposable
    {
#pragma warning disable CA2254

        private enum ResponseType
        {
            AllCorrect = 200,
            AllFailed = 400,
            CreateCorrectCloseFailed = 300,
            CreateCorrectClosePartiallyFailed = 301,
            CreateFailedCloseCorrect = 302,
            CreateFailedClosePartiallyFailed = 303,
        }

        private const string GET_INSTRUMENTS_PATH       = "/trading/get_instruments";
        private const string OPEN_TRADE_PATH            = "/trading/open_trade";
        private const string CLOSE_TRADE_PATH           = "/trading/close_trade";
        private const string CLOSE_ALL_FOR_SYMBOL_PATH  = "/trading/close_all_for_symbol";
        private const string GET_OPEN_POSITIONS_PATH    = "/trading/get_model/?models=OpenPosition";

        private const string HEDGE_DIFFERENTIATOR       = "HEDGE";

        public bool IsRunning { get; private set; } = true;
        public bool IsClientSetUp { get; private set; } = false;
        public bool IsSocketSetUp { get; private set; } = false;
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

        public async Task<OpenCloseTradeResponse> OpenTrade(TradersViewBuySellRequest data)
        {
            this._logger.LogTrace("OpenTrade requested");

            if (data._isHedge)
                return await OpenHedgeTransaction(data);
            else
                return await OpenRegularTrade(data);
        }

        public async Task<OpenCloseTradeResponse> CloseTrade(CloseTradeRequest data)
        {
            this._logger.LogTrace("CloseTrade requested");

            return await CloseTrade(data.tradeId, data.amount);
        }

        public async Task<OpenCloseTradeResponse> CloseTrade(CloseTradeMultipleRequest data)
        {
            this._logger.LogTrace("CloseTradeMultiple requested");

            Dictionary<string, bool> transactionsClosed = new();

            foreach (var t in data.tradeIdAmountPairs)
            {
                OpenCloseTradeResponse closeTradeResp = await CloseTrade(t.Key, t.Value);
                string orderId = t.Key;
                bool executed = false;
                if(closeTradeResp != null 
                    && closeTradeResp.data != null
                    && closeTradeResp.data.orderId != null)
                {
                    orderId = closeTradeResp.data.orderId;
                    executed = closeTradeResp.response.executed;
                }
                transactionsClosed.Add(orderId, executed);
            }
            
            return PrepareOpenCloseTradeResponse(true, -2, "-2", $"closedTransactions: {JsonConvert.SerializeObject(transactionsClosed)}");
        }

        public async Task<OpenCloseTradeResponse> CloseTrade(string tradeId, double amount)
        {
            this._logger.LogTrace("CloseTrade2 requested");

            BrokerCloseTradeMessage requestBody = new(this.OrderType, this.TimeInForce, 
                                                        tradeId, null, null, amount);

            HttpRequestMessage request = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{this.BaseUrl}{CLOSE_TRADE_PATH}"),
                Content = new FormUrlEncodedContent(requestBody.ToKeyValuePairs())
            };

            this._logger.LogTrace($"sending CloseTrade request: {request}");

            SetHeaders();
            HttpResponseMessage? message = await this.Client.SendAsync(request);

            return await HandleBrokersResponse(message);
        }

        public async Task<OpenCloseTradeResponse> CloseAllForSymbol(string symbol)
        {
            this._logger.LogTrace("CloseAllForSymbol requested");

            BrokerCloseAllForSymbolMessage requestBody = new(this.AccountId, symbol, this.OrderType, this.TimeInForce);

            HttpRequestMessage request = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{this.BaseUrl}{CLOSE_ALL_FOR_SYMBOL_PATH}"),
                Content = new FormUrlEncodedContent(requestBody.ToKeyValuePairs())
            };

            this._logger.LogTrace($"sending CloseAllForSymbol request: {request}");

            SetHeaders();
            HttpResponseMessage? message = await this.Client.SendAsync(request);

            return await HandleBrokersResponse(message);
        }

        public async Task<string> GetInstruments()
        {
            this._logger.LogTrace("GetInstruments requested");

            HttpRequestMessage request = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{this.BaseUrl}{GET_INSTRUMENTS_PATH}"),
            };

            this._logger.LogTrace($"sending GetInstruments request: {request}");

            SetHeaders();
            HttpResponseMessage? message = await this.Client.SendAsync(request);

            string apiResponse;
            if (message == null || !message.IsSuccessStatusCode || message.Content == null)
            {
                apiResponse = $"request failed: {message}";
                this._logger.LogTrace($"BROKER: {JsonConvert.SerializeObject(apiResponse)}");
            }
            else
            {
                apiResponse = await message.Content.ReadAsStringAsync();
                this._logger.LogTrace($"BROKER: {JsonConvert.SerializeObject(apiResponse)}");
            }

            return apiResponse;
        }

        public async Task<SnapshotResponse> GetOpenPositionSnapshot()
        {
            this._logger.LogTrace("GetOpenPositionSnapshot requested");
            HttpRequestMessage request = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{this.BaseUrl}{GET_OPEN_POSITIONS_PATH}"),
            };

            this._logger.LogTrace($"sending GetOpenPositionSnapshot request: {request}");

            SetHeaders();
            HttpResponseMessage? message = await this.Client.SendAsync(request);

            SnapshotResponse apiResponse;
            if (message == null || !message.IsSuccessStatusCode || message.Content == null)
            {
                apiResponse = new(null, $"request failed: {message}");
                this._logger.LogTrace($"BROKER: {JsonConvert.SerializeObject(apiResponse)}");
            }
            else
            {
                Snapshot? deserializedTemp = JsonConvert.DeserializeObject<Snapshot>(await message.Content.ReadAsStringAsync());
                apiResponse = new(deserializedTemp);
                this._logger.LogTrace($"{JsonConvert.SerializeObject(apiResponse)}");
            }

            return apiResponse;
        }

        private async Task<OpenCloseTradeResponse> OpenHedgeTransaction(TradersViewBuySellRequest data)
        {
            this._logger.LogTrace("OpenHedgeTransaction requested");

            string name = data._symbol.ticker;
            bool isBuy = data._is_buy == "buy";
            double amount = data._contracts.orders;

            OpenCloseTradeResponse response;
            IEnumerable<SR_OpenPosition> openPositions = (await GetOpenPositionSnapshot()).OpenPositions
                .Where(op => op.OpenOrderRequestTXT.Contains(HEDGE_DIFFERENTIATOR))
                .Where(op => op.currency == name)
                .Where(op => op.isBuy != isBuy);

            if (openPositions.Any())
            {
                Dictionary<string, bool> transactionsClosed = new();

                foreach (var position in openPositions)
                {
                    OpenCloseTradeResponse closeTradeResp = await CloseTrade(position.tradeId, position.amountK * 1000 );
                    transactionsClosed.Add(position.tradeId, (closeTradeResp?.response?.executed) ?? false);
                }
                OpenCloseTradeResponse openTradeResp = await OpenRegularTrade(data);
                string openTransactionId = openTradeResp.response.executed ? openTradeResp.data.orderId : "-1";

                bool allCloseExecuted = transactionsClosed.All(x => x.Value);
                bool anyCloseExecuted = transactionsClosed.Any(x => x.Value);

                bool executed = openTradeResp.response.executed || allCloseExecuted;
                ResponseType responseCode = GetResponseType(openTradeResp.response.executed, allCloseExecuted, anyCloseExecuted);
                string ct = $"closedTransactions: {JsonConvert.SerializeObject(transactionsClosed)}";

                response = PrepareOpenCloseTradeResponse(executed, (int)responseCode, openTransactionId, ct);
            }
            else
            {
                response = await OpenRegularTrade(data);
                if (!response.response.executed || response.data == null)
                    return response;
            }          

            return response;
        }

        private async Task<OpenCloseTradeResponse> OpenRegularTrade(TradersViewBuySellRequest data)
        {
            this._logger.LogTrace("OpenRegularTrade requested");


            BrokerOpenTradeMessage requestBody = new(this.AccountId.ToString(), this.OrderType,
                                                        this.TimeInForce, data);

            HttpRequestMessage request = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{this.BaseUrl}{OPEN_TRADE_PATH}"),
                Content = new FormUrlEncodedContent(requestBody.ToKeyValuePairs())
            };
            SetHeaders();
            HttpResponseMessage? message = await this.Client.SendAsync(request);

            return await HandleBrokersResponse(message);
        }

        private static ResponseType GetResponseType(bool executed, bool allCloseExecuted, bool anyCloseExecuted)
        {
            return executed
                ? allCloseExecuted ? ResponseType.AllCorrect : anyCloseExecuted
                    ? ResponseType.CreateCorrectClosePartiallyFailed : ResponseType.CreateCorrectCloseFailed
                : allCloseExecuted ? ResponseType.CreateFailedCloseCorrect : allCloseExecuted
                    ? ResponseType.CreateFailedClosePartiallyFailed : ResponseType.AllFailed;
        }

        private async Task<OpenCloseTradeResponse> HandleBrokersResponse(HttpResponseMessage? message, string additionalInfo = "")
        {
            OpenCloseTradeResponse apiResponse;
            if (message == null || !message.IsSuccessStatusCode || message.Content == null)
            {
                apiResponse = PrepareErrorMessage(message);
                this._logger.LogTrace($"BROKER: {JsonConvert.SerializeObject(apiResponse)};\n{additionalInfo}");
            }
            else
            {
                OpenCloseTradeResponse? deserializedTemp = JsonConvert.DeserializeObject<OpenCloseTradeResponse>(await message.Content.ReadAsStringAsync());
                apiResponse = deserializedTemp ?? PrepareErrorMessage(message);
                this._logger.LogTrace($"{JsonConvert.SerializeObject(apiResponse)};\n{ additionalInfo}");
            }

            return apiResponse;
        }

        private static OpenCloseTradeResponse PrepareErrorMessage(HttpResponseMessage? message)
        {
            return PrepareOpenCloseTradeResponse(false, -1, "-1", message != null ? message.ToString() : "message is null");
        }

        private static OpenCloseTradeResponse PrepareOpenCloseTradeResponse(bool executed, int type, string transactionId, string message)
        {
            return new OpenCloseTradeResponse(new OCTR_Response(executed, message),
                    new OCTR_Data(type, transactionId));
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

        private void SocketClose()
        {
            this.Socket.DisconnectAsync().GetAwaiter().GetResult();
            this._logger.LogTrace("Socket closed.");
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
            this._logger.LogError($"Socket Errored: {sender} ;{e}");
        }

        private void OnSocketConnected(object? sender, EventArgs e)
        {
            this.BearerToken = $"{Socket.Id}{this.Token}";
            SetAuthHeader();
            this._logger.LogTrace($"connected: {this.Socket.Connected}.");
            this._logger.LogTrace($"token: {this.BearerToken}.");
            this.IsSocketSetUp = true;
        }

        private void OnSocketDisconnected(object? sender, string e)
        {
            this._logger.LogTrace($"Socket Disconnected: {sender} ;{e}");
            if (this.IsRunning) SocketSetUp();
        }

        public void Dispose()
        {
            this.IsRunning = false;
            SocketClose();
        }
#pragma warning restore CA2254 
    }
}
