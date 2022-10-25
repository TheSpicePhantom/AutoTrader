using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using SocketIOClient;

namespace AutoTrader.Controllers
{
    public class SocketUsingControllerBase : Controller
    {
        protected SocketIO _socket;
        protected HttpClient _httpClient;
        protected string _bearerToken;
        protected string _baseUrl;
        protected string _token;

        private SocketUsingControllerBase()
        {
            this._httpClient = new HttpClient();
        }

        protected SocketUsingControllerBase(IConfiguration config)
        {
            this._httpClient = new HttpClient();
        }

        protected SocketUsingControllerBase(string baseUrl, string token)
        {
            this._httpClient = new HttpClient();
            _baseUrl = baseUrl;
            _token = token;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            SocketSetUp();
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            SocketClose();
            base.OnActionExecuted(context);
        }

        private void SocketSetUp()
        {
            this._socket = new SocketIO(this._baseUrl, new SocketIOOptions
            {
                Query = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("access_token", this._token)
                }
            });
            this._socket.OnConnected += OnSocketConnected;
            this._socket.OnError += OnSocketErrored;
            this._socket.ConnectAsync().GetAwaiter().GetResult();
        }

        private async void SocketClose()
        {
            await this._socket.DisconnectAsync();
            Console.WriteLine("Socket closed.");
        }

        private void OnSocketErrored(object? sender, string e)
        {
            Console.WriteLine($"Socket Errored: {sender} ;{e}");
        }

        private void OnSocketConnected(object? sender, EventArgs e)
        {
            this._bearerToken = $"Bearer {this._socket.Id}{this._token}";
            Console.WriteLine($"connected: {this._socket.Connected}.");
            Console.WriteLine($"token: {this._bearerToken}.");
        }
    }
}
