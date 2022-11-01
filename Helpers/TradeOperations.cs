using AutoTrader.Models;

namespace AutoTrader.Helpers
{
    public static class TradeOperations
    {
        public static void SetHeaders(HttpClient client)
        {
            SetHeader(client, "Accept", "application/json");
            SetHeader(client, "User-Agent", "request");
            //request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
        }

        public static void SetAuthHeader(HttpClient client, string bearerToken)
        {
            if (client.DefaultRequestHeaders.Contains("Authorization"))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            }
            else
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
            }
        }

        public static void SetHeader(HttpClient client, string header, string value)
        {
            if (client.DefaultRequestHeaders.Contains(header))
            {
                client.DefaultRequestHeaders.Remove(header);
                client.DefaultRequestHeaders.Add(header, value);
            }
            else
            {
                client.DefaultRequestHeaders.Add(header, value);
            }
        }
    }
}
