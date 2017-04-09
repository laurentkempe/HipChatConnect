using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HipChatConnect.Services;
using Microsoft.Extensions.Logging;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    class HipChatRoom : IHipChatRoom
    {
        private readonly ITenantService _tenantService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<HipChatRoom> _logger;

        public HipChatRoom(ITenantService tenantService, HttpClient httpClient, ILogger<HipChatRoom> logger)
        {
            _tenantService = tenantService;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendMessageAsync(MessageData messageData, string oauthId)
        {
            await Send(oauthId, messageData.Json);
        }

        public async Task SendActivityCardAsync(ActivityCardData activityCardData, string oauthId)
        {
            var content = $@"
            {{
                ""message"" : ""{activityCardData.ActivityHtml}"",
                ""message_format"" : ""html"",
                ""card"" : {activityCardData.Json}
            }}";

            await Send(oauthId, content);
        }

        private async Task Send(string oauthId, string content)
        {
            var installationData = await _tenantService.GetInstallationDataAsync(oauthId);
            var accessToken = await _tenantService.GetAccessTokenAsync(oauthId);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.access_token);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var stringContent = new StringContent(content);
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var roomGlanceUpdateUri = new Uri($"{installationData.apiUrl}room/{installationData.roomId}/notification");

            _logger.LogInformation($"Sending [{stringContent}] to {roomGlanceUpdateUri}");

            var httpResponseMessage = await _httpClient.PostAsync(roomGlanceUpdateUri, stringContent);

            _logger.LogInformation($"PostAsync result {httpResponseMessage.StatusCode}, {httpResponseMessage.ReasonPhrase}");

            httpResponseMessage.EnsureSuccessStatusCode();
        }
    }
}