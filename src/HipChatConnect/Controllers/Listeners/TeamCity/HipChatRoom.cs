using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HipChatConnect.Core.Cache;
using HipChatConnect.Models;
using Newtonsoft.Json;
using Nubot.Plugins.Samples.HipChatConnect.Models;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    class HipChatRoom : IHipChatRoom
    {
        private readonly ICache _cache;
        private readonly HttpClient _httpClient;

        public HipChatRoom(ICache cache, HttpClient httpClient)
        {
            _cache = cache;
            _httpClient = httpClient;
        }

        public async Task SendMessageAsync(string msg, InstallationData installationData)
        {
            using (var client = new HttpClient())
            {
                var accessToken = await GetAccessTokenAsync(installationData.oauthId);

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

        private async Task<AccessToken> GetAccessTokenAsync(string oauthId)
        {
            var authenticationData = await _cache.GetAsync<TenantData>(oauthId);

            if (IsExpired(authenticationData.Token))
            {
                var accessToken = await RefreshAccessToken(oauthId);
                return accessToken.Token;
            }

            return await Task.FromResult(authenticationData.Token.Token);
        }

        private bool IsExpired(ExpiringAccessToken accessToken)
        {
            return accessToken == null || accessToken.ExpirationTimeStamp < DateTime.Now;
        }

        private async Task<ExpiringAccessToken> RefreshAccessToken(string oauthId)
        {
            var authenticationData = await GetTenantDataFromCacheAsync(oauthId);

            var client = new HttpClient();

            var dataContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "send_notification")
            });

            var credentials = Encoding.ASCII.GetBytes($"{authenticationData.InstallationData.oauthId}:{authenticationData.InstallationData.oauthSecret}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            var tokenResponse = await client.PostAsync(new Uri(authenticationData.InstallationData.tokenUrl), dataContent);
            var accessToken = await tokenResponse.Content.ReadAsAsync<AccessToken>();

            var expiringAccessToken = new ExpiringAccessToken
            {
                Token = accessToken,
                ExpirationTimeStamp = DateTime.Now + TimeSpan.FromTicks((accessToken.expires_in - 60) * 1000)
            };

            authenticationData.Token = expiringAccessToken;
            await _cache.SetAsync(oauthId, authenticationData);

            return expiringAccessToken;
        }
        private async Task<TenantData> GetTenantDataFromCacheAsync(string oauthId)
        {
            return await _cache.GetAsync<TenantData>(oauthId);
        }

        public async Task SendMessageAsync(string msg, string oauthId)
        {
            var tenantData = await GetTenantDataFromCacheAsync(oauthId);
            var accessToken = await GetAccessTokenAsync(oauthId);

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

            var roomGlanceUpdateUri = new Uri($"{tenantData.InstallationData.apiUrl}room/{tenantData.InstallationData.roomId}/notification");
            var httpResponseMessage = await _httpClient.PostAsync(roomGlanceUpdateUri, stringContent);
            httpResponseMessage.EnsureSuccessStatusCode();
        }
    }
}