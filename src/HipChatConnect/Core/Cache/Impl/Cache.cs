using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace HipChatConnect.Core.Cache.Impl
{
    internal class Cache : ICache
    {
        private readonly IDatabase _cache;

        public Cache(IDatabase cache)
        {
            _cache = cache;
        }

        public async Task SetAsync<T>(string key, T value)
        {
            await _cache.StringSetAsync(key, JsonConvert.SerializeObject(value));
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var json = await _cache.StringGetAsync(key);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.KeyDeleteAsync(key);
        }
    }
}