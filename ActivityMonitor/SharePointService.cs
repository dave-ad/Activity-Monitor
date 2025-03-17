using ActivityMonitor.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ActivityMonitor
{
    public class SharePointService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tenantId;
        private readonly string _siteId;
        private readonly string _listId;
        private readonly HttpClient _httpClient;

        public SharePointService(IConfiguration configuration, HttpClient httpClient)
        {
            var sharePointSettings = configuration.GetSection("SharePointSettings");
            _clientId = sharePointSettings["ClientId"];
            _clientSecret = sharePointSettings["ClientSecret"];
            _tenantId = sharePointSettings["TenantId"];
            _siteId = sharePointSettings["SiteId"];
            _listId = sharePointSettings["ListId"];
            _httpClient = httpClient;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var cca = ConfidentialClientApplicationBuilder.Create(_clientId)
                .WithClientSecret(_clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{_tenantId}"))
                .Build();

            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var result = await cca.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }

        public async Task<string> UploadScreenshotAsync(byte[] screenshotBytes, string fileName)
        {
            var accessToken = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // SharePoint document library URL
            string uploadUrl = $"https://graph.microsoft.com/v1.0/sites/{_siteId}/drive/root:/Screenshots/{fileName}:/content";

            using (var content = new ByteArrayContent(screenshotBytes)) // Explicit using block
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                var response = await _httpClient.PutAsync(uploadUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Error uploading image: " + errorResponse);
                    return null;
                }

                // Get uploaded file metadata
                var responseBody = await response.Content.ReadAsStringAsync();
                dynamic uploadedFile = JsonConvert.DeserializeObject(responseBody);
                return uploadedFile.webUrl; // This is the SharePoint URL of the uploaded image
            } // Content is automatically disposed here
        }


        public async Task<HttpResponseMessage> AddItemAsync(ActivityLog model)
        {
            var accessToken = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Upload screenshot & get SharePoint URL
            string screenshotUrl = null;
            if (model.ScreenshotByteArray != null && model.ScreenshotByteArray.Length > 0)
            {
                screenshotUrl = await UploadScreenshotAsync(model.ScreenshotByteArray, $"screenshot_{DateTime.UtcNow.Ticks}.png");
            }

            var itemUrl = $"https://graph.microsoft.com/v1.0/sites/{_siteId}/lists/{_listId}/items";
            var itemData = new
            {
                fields = new
                {
                    Title = model.ApplicationName,
                    Description = model.Description,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    WebsiteUrl = model.WebsiteUrl,
                    IsIdle = model.IsIdle,
                    User = model.User,
                    ScreenshotUrl = screenshotUrl // Store the uploaded file URL
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(itemData), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(itemUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error Response: " + responseBody);
            }

            return response;
        }

    }
}