namespace AutoTrader.Models
{
    public class BrokerCloseAllForSymbolMessage
    {
        public string AccountId { get; set; } 
        public string Symbol { get; set; } 
        public string OrderType { get; set; }
        public string TimeInForce { get; set; }

        public BrokerCloseAllForSymbolMessage(string accountId, string symbol, string orderType, string timeInForce)
        {
            this.AccountId = accountId;
            this.Symbol = symbol;
            this.OrderType = orderType;
            this.TimeInForce = timeInForce;
        }

        public IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs()
        {
            IList<KeyValuePair<string, string>> kvpairs = new List<KeyValuePair<string, string>>()
            {
                new("account_id"    , this.AccountId),
                new("for_Symbol"     , true.ToString()),
                new("symbol"        , this.Symbol),
                new("order_type"    , this.OrderType),
                new("time_in_force" , this.TimeInForce)
            };

            return kvpairs;
        }
    }
}
