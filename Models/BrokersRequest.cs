namespace AutoTrader.Models
{
    public class BrokersRequest
    {
        public string accountId { get; set; }
        public string symbol { get; set; }
        public bool isBuy { get; set; }
        public long amount { get; set; }
        public string orderType { get; set; }
        public string requestText { get; set; }
        public string timeInForce { get; set; }

        BrokersRequest(string accountId, string orderType, string timeInForce, TradersViewRequest reqBody)
        {
            if (reqBody._is_buy != "buy" && reqBody._is_buy != "sell") throw new ArgumentException("error: invalid parameter _is_buy");
            if (reqBody._contracts == null || reqBody._contracts.orders != 0) throw new ArgumentException("error: invalid parameter _contracts/orders");
            if (reqBody._symbol == null || reqBody._symbol.exchange == null) throw new ArgumentException("error: invalid parameter _symbol/exchange");
             
            this.accountId = accountId;
            this.symbol = reqBody._symbol.exchange;         // also has a "ticker": "{{ticker}}", which is a time of the update ?
            this.isBuy = reqBody._is_buy == "buy";          // if not buy then sell
            this.amount = reqBody._contracts.orders;        // amount of transaction

            this.orderType = orderType;                     // something 
            this.timeInForce = timeInForce;                 // something
            this.requestText = reqBody._comment;            // doesn"t really matter prob like a logging thing
        }
    }
}
