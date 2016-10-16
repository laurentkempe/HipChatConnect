using System.Threading.Tasks;

namespace HipChatConnect.Core.Cache
{
    public interface ICache
    {
        Task SetAsync<T>(string key, T value);
        Task<T> GetAsync<T>(string key);
        Task RemoveAsync(string key);
    }
}