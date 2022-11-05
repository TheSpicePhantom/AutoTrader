namespace AutoTrader.Models
{
    public record CloseTradeMultipleRequest(IDictionary<string, double> tradeIdAmountPairs);
    public record CloseTradeRequest(string tradeId, double amount);

}
