using System.Collections.Concurrent;

namespace Smartstore.Core.Seo
{
    public class EntitySemaphoreSlim
    {
        private readonly ConcurrentDictionary<Type, SemaphoreSlim> semaphoreDictionary = new ConcurrentDictionary<Type, SemaphoreSlim>();
        private readonly SemaphoreSlim encompassingSemaphore = new SemaphoreSlim(1, 1);

        public async Task<SemaphoreSlim> GetEventAsync<T>()
        {
            await encompassingSemaphore.WaitAsync();

            try
            {
                return semaphoreDictionary.GetOrAdd(typeof(T), _ => new SemaphoreSlim(1, 1));
            }
            finally
            {
                encompassingSemaphore.Release();
            }
        }

        public async Task<SemaphoreSlim> GetEventAsync<T>(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return await GetEventAsync<T>();
        }

        public async Task<SemaphoreSlim> GetEventAsync(Type type)
        {
            if (type == null)
            {
                throw new ArgumentException(nameof(type));
            }

            await encompassingSemaphore.WaitAsync();

            try
            {
                return semaphoreDictionary.GetOrAdd(type, _ => new SemaphoreSlim(1, 1));
            }
            finally
            {
                encompassingSemaphore.Release();
            }
        }

        public async Task BlockAll()
        {
            await encompassingSemaphore.WaitAsync();
        }

        public void ReleaseAll()
        {
            encompassingSemaphore.Release();
        }
    }
}