namespace AutoTrader.Models
{
    public class TradersViewRequest
    {
        public string _is_buy { get; set; }
        public Symbol _symbol { get; set; }
        public Contract _contracts { get; set; }
        public long _price { get; set; }
        public DateTime _timestamp { get; set; }
        public string _comment { get; set; }
    }

    public class Symbol
    {
        public long ticker { get; set; }
        public string exchange { get; set; }
    }

    public class Contract
    {
        public long orders { get; set; }
    }
}
