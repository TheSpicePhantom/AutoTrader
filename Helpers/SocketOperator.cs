using AutoTrader.Models;
using SocketIOClient;

namespace AutoTrader.Helpers
{
    public sealed class SocketOperator
    {
        public HttpClient Client { get; private set; }
        public bool IsClientSetUp { get; private set; } = false;
        public SocketIO Socket { get; private set; }
        public bool IsSocketSetUp { get; private set; } = false;
        public string BearerToken { get; private set; }
        public string Token { get; private set; }

        private List<OpenTransaction> _transactions = new();


        private IConfiguration _config;
        private ILogger _logger;

        public SocketOperator(IConfiguration config, ILogger<SocketOperator> logger)
        {
            Program.ApplicationShuttingDown += Program_ApplicationShuttingDown;

            this._logger = logger;
            this._config = config.GetSection("BrokerConfig");

            this.Token = this._config.GetValue<string>("token");
            
            SetUpClient();
            SocketSetUp(this._config.GetValue<string>("baseUrl"), this.Token);
        }

        public OpenTransaction? GetTransation(string name)
        {
            return this._transactions.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public void OpenNewTransaction(string name, bool isBuy, string transactionId)
        {

        }


        private void SetUpClient()
        {
            Client = new HttpClient();
            IsClientSetUp = true;
        }

        private void SocketSetUp(string? baseUrl, string? token)
        {
            if (baseUrl == null || token == null || this == null) return;

            if (IsSocketSetUp) return;

            Socket = new SocketIO(baseUrl, new SocketIOOptions
            {
                Query = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("access_token", token)
                }
            });
            Socket.OnConnected += this.OnSocketConnected;
            Socket.OnError += this.OnSocketErrored;
            Socket.ConnectAsync().GetAwaiter().GetResult();
        }

        private async void SocketClose()
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
            this.BearerToken = $"{Socket.Id}{this.Token}";
            TradeOperations.SetAuthHeader(Client, this.BearerToken);
            Console.WriteLine($"connected: {Socket.Connected}.");
            Console.WriteLine($"token: {this.BearerToken}.");
            this._logger.LogInformation($"connected: {Socket.Connected}.");
            this._logger.LogInformation($"token: {this.BearerToken}.");
            IsSocketSetUp = true;
        }

        private void Program_ApplicationShuttingDown()
        {
            SocketClose();
        }
    }
}
