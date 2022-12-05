namespace AutoTrader.Models
{
    public record TradersViewBuySellRequest(string _is_buy, TVBSR_Symbol _symbol, TVBSR_Contract _contracts, double _price, DateTime? _timestamp, string _comment, bool _isHedge);
    public record TVBSR_Symbol(string ticker, string exchange);
    public record TVBSR_Contract(double orders);
}
