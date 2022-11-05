namespace AutoTrader.Models
{
    public record OpenCloseTradeResponse(OCTR_Response response, OCTR_Data data);
    public record OCTR_Response(bool executed);
    public record OCTR_Data(int type, string orderId, string? message);
}
