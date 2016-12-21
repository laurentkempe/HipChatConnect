using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HipChatConnect.Core.Models;
using HipChatConnect.Services;
using Newtonsoft.Json;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    class HipChatRoom : IHipChatRoom
    {
        private readonly ITenantService _tenantService;
        private readonly HttpClient _httpClient;

        public HipChatRoom(ITenantService tenantService, HttpClient httpClient)
        {
            _tenantService = tenantService;
            _httpClient = httpClient;
        }

        public async Task SendMessageAsync(string msg, InstallationData installationData)
        {
            using (var client = new HttpClient())
            {
                var accessToken = await _tenantService.GetAccessTokenAsync(installationData.oauthId);

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.access_token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var messageData = new
                {
                    color = "gray",
                    message = msg,
                    message_format = "html"
                };

                var stringContent = new StringContent(JsonConvert.SerializeObject(messageData));
                stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var roomGlanceUpdateUri = new Uri($"{installationData.apiUrl}room/{installationData.roomId}/notification");
                var httpResponseMessage = await client.PostAsync(roomGlanceUpdateUri, stringContent);
                httpResponseMessage.EnsureSuccessStatusCode();
            }
        }

        public async Task SendMessageAsync(string msg, string oauthId)
        {
            var installationData = await _tenantService.GetInstallationDataAsync(oauthId);
            var accessToken = await _tenantService.GetAccessTokenAsync(oauthId);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.access_token);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var messageData = new
            {
                color = "gray",
                message = msg,
                message_format = "html"
            };

            var stringContent = new StringContent(JsonConvert.SerializeObject(messageData));
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var roomGlanceUpdateUri = new Uri($"{installationData.apiUrl}room/{installationData.roomId}/notification");
            var httpResponseMessage = await _httpClient.PostAsync(roomGlanceUpdateUri, stringContent);
            httpResponseMessage.EnsureSuccessStatusCode();
        }
    }
}