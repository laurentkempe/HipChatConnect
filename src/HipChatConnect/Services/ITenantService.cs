using System.Collections.Generic;
using System.Threading.Tasks;
using HipChatConnect.Core.Models;

namespace HipChatConnect.Services
{
    public interface ITenantService
    {
        Task<bool> ValidateTokenAsync(string jwtToken);

        Task CreateAsync(InstallationData installationData);

        Task<AccessToken> GetAccessTokenAsync(string oauthId);

        Task RemoveAsync(string oauthId);

        Task<InstallationData> GetInstallationDataAsync(string oauthId);

        Task<IEnumerable<IConfiguration<T>>> GetAllConfigurationAsync<T>();

        Task SetConfigurationAsync<T>(string jwtToken, T data);

        Task<T> GetConfigurationAsync<T>(string oauthId) where T : new();
    }
}