namespace AutoTrader.Models
{
    public record OpenTransaction
    {
        public string Name { get; set; } = "Empty Transaction";
        public List<string> BuyOrders { get; set; } = new();
        public List<string> SellOrders { get; set; } = new();
    }
}
