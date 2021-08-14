using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FakeClientLibrary
{
    public static class FakeClient
    {
        public static async Task Run(string baseUrl, bool waitSixSecondsAfterSecondAddRequest = false)
        {
            var urlForStartTransaction = Combine(baseUrl, "StartTransaction");
            var urlForAdd = Combine(baseUrl, "Add");
            var urlForEndTransaction = Combine(baseUrl, "EndTransaction");

            var httpClient = new HttpClient();

            var responseStr = await Request(httpClient, urlForStartTransaction, "");

            var transactionId = JsonConvert.DeserializeObject<Guid>(responseStr);

            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(waitSixSecondsAfterSecondAddRequest && i == 2 ? 6 : 1));

                await Request(httpClient, urlForAdd + "?transactionId=" + transactionId, JsonConvert.SerializeObject(new {Value = "Value" + i}));
            }

            await Task.Delay(TimeSpan.FromSeconds(1));

            await Request(httpClient, urlForEndTransaction + "?transactionId=" + transactionId, "");

        }

        private static async Task<string> Request(HttpClient httpClient, string url, string requestStr)
        {
            var response = await httpClient.PostAsync(url, new StringContent(requestStr, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private static string Combine(string uri1, string uri2)
        {
            uri1 = uri1.TrimEnd('/');
            uri2 = uri2.TrimStart('/');
            return $"{uri1}/{uri2}";
        }
    }
}
