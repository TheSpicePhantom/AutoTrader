using Newtonsoft.Json;

namespace AutoTrader.Models
{
    public class BrokerConfig
    {
        public long AccountId { get; set; }
        public string BaseUrl { get; set; }
        public string TimeInForce { get; set; }
        public string OrderType { get; set; }
        public string Token { get; set; }


        public BrokerConfig(IConfigurationSection cfg)
        {
            this.AccountId      = cfg.GetValue<long>("accountId");
            this.BaseUrl        = cfg.GetValue<string>("baseUrl");
            this.TimeInForce    = cfg.GetValue<string>("timeInForce");
            this.OrderType      = cfg.GetValue<string>("orderType");
            this.Token          = cfg.GetValue<string>("token");
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
