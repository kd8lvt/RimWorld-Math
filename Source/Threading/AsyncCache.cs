using System;
using System.Threading.Tasks;
using LazyCache;

namespace CrunchyDuck.Math.Threading
{
    public class AsyncCache<T>
    {
        private IAppCache cache;
        private Func<Task<T>> taskToCache;
        private string id;
        public AsyncCache(Func<T> func, string id)
        {
            this.id = id;
            cache = new CachingService();
            taskToCache = () =>
            {
                Task<T> task = new Task<T>(()=>
                {
                    return func.Invoke();
                });
                return task;
            };
        }

        public async Task<T> Get()
        {
            return await cache.GetOrAddAsync(id, taskToCache, DateTimeOffset.Now.AddSeconds(1));
        }
    }
}
