namespace AutoTrader.Models
{
#pragma warning disable CS8618
    public class TradersViewRequest
    {
        public string _is_buy { get; set; }
        public Symbol _symbol { get; set; }
        public Contract _contracts { get; set; }
        public double _price { get; set; }
        public long _timestamp { get; set; }
        public string _comment { get; set; }
    }

    public class Symbol
    {
        public long ticker { get; set; }
        public string exchange { get; set; }
    }

    public class Contract
    {
        public double orders { get; set; }
    }
#pragma warning restore CS8618
}
