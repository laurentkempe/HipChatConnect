using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace HipChatConnect.Core.Cache.Impl
{
    internal class Cache : ICache
    {
        private readonly IDistributedCache _cache;

        public Cache(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task SetAsync<T>(string key, T value)
        {
            await _cache.SetAsync(key, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)));
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var bytes = await _cache.GetAsync(key);

            var json = Encoding.UTF8.GetString(bytes);
            return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<T>(json));
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }
    }
}