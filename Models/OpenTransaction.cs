namespace AutoTrader.Models
{
    public record OpenTransaction(string Name, Dictionary<string, double> BuyOrders, Dictionary<string, double> SellOrders);
}
