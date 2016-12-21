using System.Collections.Generic;
using System.Threading.Tasks;
using HipChatConnect.Core.Models;

namespace HipChatConnect.Services
{
    public interface ITenantService
    {
        Task<bool> ValidateTokenAsync(string jwt);

        Task CreateAsync(InstallationData installationData);

        Task<AccessToken> GetAccessTokenAsync(string oauthId);

        Task<Dictionary<string, string>> GetConfigurationAsync(string oauthId);

        Task RemoveAsync(string oauthId);

        Task SetConfigurationAsync(string jwtToken, string key, string value);

        Task<InstallationData> GetInstallationDataAsync(string oauthId);
    }
}