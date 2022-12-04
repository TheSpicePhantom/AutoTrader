namespace AutoTrader.Models
{
    public class BrokerOpenTradeMessage
    {
        public string AccountId { get; set; }
        public string Symbol { get; set; }
        public bool IsBuy { get; set; }
        public double Amount { get; set; }
        public string OrderType { get; set; }
        public string RequestText { get; set; }
        public string TimeInForce { get; set; }
        public bool IsHedge { get; set; }

        public BrokerOpenTradeMessage(string accountId, string orderType, string timeInForce, TradersViewBuySellRequest reqBody)
        {
            if (reqBody._is_buy != "buy" && reqBody._is_buy != "sell") throw new ArgumentException("error: invalid parameter _is_buy");
            if (reqBody._contracts == null || reqBody._contracts.orders <= 0) throw new ArgumentException("error: invalid parameter _contracts/orders");
            if (reqBody._symbol == null || reqBody._symbol.exchange == null) throw new ArgumentException("error: invalid parameter _symbol/exchange");
             
            this.AccountId      = accountId;
            this.Symbol         = $"{reqBody._symbol.ticker}";                              // also has a "exchange": "{{exchange}}", which is a like a market or something
            this.IsBuy          = reqBody._is_buy == "buy";                                 // if not buy then sell
            this.Amount         = reqBody._contracts.orders;                                // amount of transaction
            this.OrderType      = orderType;                                                // something 
            this.TimeInForce    = timeInForce;                                              // something
            this.RequestText    = reqBody._comment;                                         // doesn"t really matter prob like a logging thing
            this.IsHedge        = reqBody._is_hedge;                                        // for differentiating types of trade
        }

        public IEnumerable<KeyValuePair<string,string>> ToKeyValuePairs()
        {
            return new List<KeyValuePair<string, string>>()
            {
                new("account_id"    , this.AccountId),
                new("symbol"        , this.Symbol),
                new("is_buy"        , this.IsBuy.ToString().ToLowerInvariant()),
                new("amount"        , this.Amount.ToString()),
                new("order_type"    , this.OrderType),
                new("request_text"  , this.RequestText),
                new("time_in_force" , this.TimeInForce)
            };
        }
    }
}
