using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ActivityMonitor.Controllers
{
    class SharePointLogger
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string siteUrl = "https://yourtenant.sharepoint.com/sites/YourSite";
        private static readonly string listName = "Logs";
        private static readonly string accessToken = "YOUR_ACCESS_TOKEN"; // Fetch via OAuth

        public static async Task SendLogToSharePoint(string logMessage)
        {
            try
            {
                string requestUrl = $"{siteUrl}/_api/web/lists/getbytitle('{listName}')/items";

                var requestBody = new
                {
                    __metadata = new { type = "SP.Data.LogsListItem" },
                    Title = "Log Entry",
                    LogMessage = logMessage,
                    CreatedDate = DateTime.UtcNow
                };

                string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                request.Headers.Add("X-RequestDigest", await GetRequestDigest());

                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                Console.WriteLine("Log sent to SharePoint successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending log: {ex.Message}");
            }
        }

        private static async Task<string> GetRequestDigest()
        {
            string digestUrl = $"{siteUrl}/_api/contextinfo";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, digestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            return jsonResponse.d.GetContextWebInformation.FormDigestValue;
        }

    }
}
