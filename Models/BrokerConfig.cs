using Newtonsoft.Json;

namespace AutoTrader.Models
{
    public class BrokerConfig
    {
        public string accountId { get; set; }
        public string baseUrl { get; set; }
        public int port { get; set; }

        public BrokerConfig(IConfigurationSection cfg)
        {
            this.accountId  = cfg.GetValue<string>("accountId");
            this.baseUrl    = cfg.GetValue<string>("baseUrl");
            this.port       = cfg.GetValue<int>("port");
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
