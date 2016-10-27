using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HipChatConnect.Core.Cache;
using HipChatConnect.Models;
using Microsoft.IdentityModel.Tokens;
using Nubot.Plugins.Samples.HipChatConnect.Models;

namespace HipChatConnect.Services.Impl
{
    public class TenantService : ITenantService
    {
        private readonly ICache _cache;
        private readonly HttpClient _httpClient;

        public TenantService(ICache cache, HttpClient httpClient)
        {
            _cache = cache;
            _httpClient = httpClient;
        }

        public async Task CreateTenantAsync(InstallationData installation)
        {
            var response = await _httpClient.GetAsync(new Uri(installation.capabilitiesUrl));
            response.EnsureSuccessStatusCode();

            var capabilitiesDocument = await response.Content.ReadAsAsync<CapabilitiesRoot>();

            installation.tokenUrl = capabilitiesDocument.capabilities.oauth2Provider.tokenUrl;
            installation.apiUrl = capabilitiesDocument.capabilities.hipchatApiProvider.url;

            await _cache.SetAsync(installation.oauthId, new TenantData { InstallationData = installation });
        }

        public async Task<bool> ValidateTokenAsync(string jwt)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var readToken = jwtSecurityTokenHandler.ReadToken(jwt);

            var authenticationData = await GetTenantDataAsync(readToken.Issuer);
            var installationData = authenticationData.InstallationData;

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = installationData.oauthId,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(installationData.oauthSecret)),
                ValidateAudience = false,
                ValidateLifetime = true
            };

            try
            {
                SecurityToken token;
                var validatedToken = jwtSecurityTokenHandler.ValidateToken(jwt, validationParameters, out token);
                return validatedToken != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<AccessToken> GetAccessTokenAsync(string oauthId)
        {
            var authenticationData = await _cache.GetAsync<TenantData>(oauthId);

            if (IsExpired(authenticationData.Token))
            {
                var accessToken = await RefreshAccessToken(oauthId);
                return accessToken.Token;
            }

            return await Task.FromResult(authenticationData.Token.Token);
        }

        public async Task<TenantData> GetTenantDataAsync(string oauthId)
        {
            return await _cache.GetAsync<TenantData>(oauthId);
        }

        public async Task RemoveAsync(string oauthId)
        {
            await _cache.RemoveAsync(oauthId);
        }

        public async Task SetTenantDataAsync(string jwtToken, string key, string value)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var readToken = jwtSecurityTokenHandler.ReadToken(jwtToken);

            var oauthId = readToken.Issuer;

            var tenantData = await GetTenantDataAsync(oauthId);

            string originalValue;
            if (tenantData.Store.TryGetValue(key, out originalValue))
            {
                tenantData.Store.Remove(key);
            }

            tenantData.Store.Add(key, value);

            await _cache.SetAsync(oauthId, tenantData);
        }

        private async Task<ExpiringAccessToken> RefreshAccessToken(string oauthId)
        {
            var authenticationData = await GetTenantDataAsync(oauthId);

            var dataContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "send_notification")
            });

            var credentials = Encoding.ASCII.GetBytes($"{authenticationData.InstallationData.oauthId}:{authenticationData.InstallationData.oauthSecret}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            var tokenResponse = await _httpClient.PostAsync(new Uri(authenticationData.InstallationData.tokenUrl), dataContent);
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

        private bool IsExpired(ExpiringAccessToken accessToken)
        {
            return accessToken == null || accessToken.ExpirationTimeStamp < DateTime.Now;
        }
    }
}