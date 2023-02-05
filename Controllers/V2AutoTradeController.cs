using Microsoft.AspNetCore.Mvc;
using fxcore2;
using AutoTrader.Helpers;

namespace AutoTrader.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class V2AutoTradeController
    {
        public string Token { get; private set; }
        public string AccountId { get; private set; }

        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        public V2AutoTradeController(IConfiguration config, ILogger<SocketOperator> logger)
        {
            _config = config;
            _logger = logger;

            this.Token = this._config.GetValue<string>("token");
            this.AccountId = this._config.GetValue<string>("accountId");
        }

        [HttpGet]
        public IActionResult Get()
        { 
            O2GSession mSession = O2GTransport.createSession();
            SessionStatusListener statusListener = new(mSession);
            mSession.subscribeSessionStatus(statusListener);
            mSession.loginWithToken(this.AccountId, this.Token, "http://www.fxcorporate.com/Hosts.jsp", "Demo");

            if (statusListener.Connected)
            { }

            mSession.logout();
            mSession.unsubscribeSessionStatus(statusListener);
            mSession.Dispose();

            return new OkObjectResult("ok");
        }
    }
}
