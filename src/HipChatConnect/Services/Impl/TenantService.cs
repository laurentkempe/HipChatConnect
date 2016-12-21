﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HipChatConnect.Core.Models;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace HipChatConnect.Services.Impl
{
    public class TenantService : ITenantService
    {
        private readonly IDatabase _database;
        private readonly HttpClient _httpClient;

        public TenantService(IDatabase database, HttpClient httpClient)
        {
            _database = database;
            _httpClient = httpClient;
        }

        public async Task CreateAsync(InstallationData installationData)
        {
            var response = await _httpClient.GetAsync(new Uri(installationData.capabilitiesUrl));
            response.EnsureSuccessStatusCode();

            var capabilitiesDocument = await response.Content.ReadAsAsync<CapabilitiesDocument>();

            installationData.tokenUrl = capabilitiesDocument.capabilities.oauth2Provider.tokenUrl;
            installationData.apiUrl = capabilitiesDocument.capabilities.hipchatApiProvider.url;

            await _database.StringSetAsync($"{installationData.oauthId}:installationData", JsonConvert.SerializeObject(installationData));
            await _database.ListLeftPushAsync("installations", installationData.oauthId);
        }

        public async Task RemoveAsync(string oauthId)
        {
            await _database.KeyDeleteAsync($"{oauthId}:installationData");
            await _database.KeyDeleteAsync($"{oauthId}:token");
            await _database.KeyDeleteAsync($"{oauthId}:configuration");
            await _database.ListRemoveAsync("installations", oauthId);
        }

        public async Task SetConfigurationAsync(string jwtToken, string key, string value)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var readToken = jwtSecurityTokenHandler.ReadToken(jwtToken);

            var oauthId = readToken.Issuer;

            var dictionary = await GetConfigurationAsync(oauthId);

            string originalValue;
            if (dictionary.TryGetValue(key, out originalValue))
                dictionary.Remove(key);

            dictionary.Add(key, value);

            await _database.StringSetAsync($"{oauthId}:configuration", JsonConvert.SerializeObject(dictionary));
        }

        public async Task<Dictionary<string, string>> GetConfigurationAsync(string oauthId)
        {
            var json = await _database.StringGetAsync($"{oauthId}:configuration");

            var dictionary = new Dictionary<string, string>();
            if (json == RedisValue.Null)
            {
                await _database.StringSetAsync($"{oauthId}:configuration", JsonConvert.SerializeObject(dictionary));
                return dictionary;
            }

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        public async Task<bool> ValidateTokenAsync(string jwt)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var readToken = jwtSecurityTokenHandler.ReadToken(jwt);

            var installationData = await GetInstallationDataAsync(readToken.Issuer);

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
            var expiringAccessToken = await GetTokenAsync(oauthId);

            if (IsExpired(expiringAccessToken))
            {
                var accessToken = await RefreshAccessToken(oauthId);
                return accessToken.Token;
            }

            return await Task.FromResult(expiringAccessToken.Token);
        }

        public async Task<InstallationData> GetInstallationDataAsync(string oauthId)
        {
            var json = await _database.StringGetAsync($"{oauthId}:installationData");

            return JsonConvert.DeserializeObject<InstallationData>(json);
        }

        private async Task<ExpiringAccessToken> GetTokenAsync(string oauthId)
        {
            var json = await _database.StringGetAsync($"{oauthId}:token");

            return JsonConvert.DeserializeObject<ExpiringAccessToken>(json);
        }

        private async Task<ExpiringAccessToken> RefreshAccessToken(string oauthId)
        {
            var installationData = await GetInstallationDataAsync(oauthId);

            var dataContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "send_notification")
            });

            var credentials =
                Encoding.ASCII.GetBytes(
                    $"{installationData.oauthId}:{installationData.oauthSecret}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(credentials));

            var tokenResponse = await _httpClient.PostAsync(new Uri(installationData.tokenUrl),
                dataContent);
            var accessToken = await tokenResponse.Content.ReadAsAsync<AccessToken>();

            var expiringAccessToken = new ExpiringAccessToken
            {
                Token = accessToken,
                ExpirationTimeStamp = DateTime.Now + TimeSpan.FromTicks((accessToken.expires_in - 60) * 1000)
            };

            await _database.StringSetAsync($"{oauthId}:token", JsonConvert.SerializeObject(expiringAccessToken));

            return expiringAccessToken;
        }

        private bool IsExpired(ExpiringAccessToken accessToken)
        {
            return accessToken == null || accessToken.ExpirationTimeStamp < DateTime.Now;
        }
    }
}