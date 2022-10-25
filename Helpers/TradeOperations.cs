namespace AutoTrader.Helpers
{
    public static class TradeOperations
    {
        public static void SetHeaders(ref HttpRequestMessage request, string bearerToken)
        {
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "request");
            //request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            request.Headers.Add("Authorization", bearerToken);
        }
    }
}
