using System.Threading.Tasks;
using HipChatConnect.Models;
using Nubot.Plugins.Samples.HipChatConnect.Models;

namespace HipChatConnect.Services
{
    public interface ITenantService
    {
        Task<bool> ValidateTokenAsync(string jwt);

        Task CreateTenantAsync(InstallationData installation);

        Task<AccessToken> GetAccessTokenAsync(string oauthId);

        Task<TenantData> GetTenantDataAsync(string oauthId);

        Task RemoveAsync(string oauthId);

        Task SetTenantDataAsync(string jwtToken, string key, string value);
    }
}