namespace AutoTrader.Models
{
#pragma warning disable CS8618

    public class OpenCloseTradeResponse
    {
        public OCTR_Response response { get; set; }
        public OCTR_Data data { get; set; }

        public OpenCloseTradeResponse() { }
        public OpenCloseTradeResponse(OCTR_Response response, OCTR_Data data) 
        {
            this.response = response;
            this.data = data;
        }
    }

    public class OCTR_Response
    {
        public bool executed { get; set; }
        public string? error { get; set; }

        public OCTR_Response() { }
        public OCTR_Response(bool executed, string? error) 
        {
            this.executed = executed;
            this.error = error;
        }
    }

    public class OCTR_Data
    {
        public int type { get; set; }
        public string orderId { get; set; }

        public OCTR_Data() { }
        public OCTR_Data(int type, string orderId)
        {
            this.type = type;
            this.orderId = orderId;
        }
    }
#pragma warning restore CS8618
}