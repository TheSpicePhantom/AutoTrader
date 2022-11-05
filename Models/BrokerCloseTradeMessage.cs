namespace AutoTrader.Models
{
    public class BrokerCloseTradeMessage
    {
        public string TradeId { get; set; }
        public double? Rate { get; set; }
        public double Amount { get; set; }
        public double? AtMarket { get; set; }
        public string OrderType { get; set; }
        public string TimeInForce { get; set; }

        public BrokerCloseTradeMessage(string orderType, string timeInForce, string tradeId, double? rate, double? atMarket, TradersViewBuySellRequest reqBody)
        {
            if (reqBody._contracts == null || reqBody._contracts.orders <= 0) throw new ArgumentException("error: invalid parameter _contracts/orders");
            if (reqBody._symbol == null || reqBody._symbol.exchange == null) throw new ArgumentException("error: invalid parameter _symbol/exchange");

            this.TradeId = tradeId;
            this.Rate = rate;
            this.Amount = reqBody._contracts.orders;
            this.AtMarket = AtMarket;
            this.OrderType = orderType;
            this.TimeInForce = timeInForce;
        }

        public IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs()
        {
            IList<KeyValuePair<string,string>> kvpairs = new List<KeyValuePair<string, string>>()
            {
                new("trade_id"      , this.TradeId),
                new("amount"        , this.Amount.ToString()),
                new("order_type"    , this.OrderType),
                new("time_in_force" , this.TimeInForce)
            };

            if (this.Rate.HasValue) kvpairs.Add(new("rate", this.Rate.Value.ToString()));
            if (this.AtMarket.HasValue) kvpairs.Add(new("at_market", this.AtMarket.Value.ToString()));

            return kvpairs;
        }
    }
}
