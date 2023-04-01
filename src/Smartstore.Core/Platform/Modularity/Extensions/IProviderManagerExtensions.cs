namespace Smartstore.Engine.Modularity
{
    public static class IProviderManagerExtensions
    {
        public static bool TryGetProvider<TProvider>(this IProviderManager manager, 
            string systemName, 
            int storeId, 
            out Provider<TProvider> provider)
            where TProvider : IProvider
        {
            Guard.NotNull(manager);
            
            provider = null;

            try
            {
                provider = manager.GetProvider<TProvider>(systemName, storeId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGetProvider(this IProviderManager manager,
            string systemName,
            int storeId,
            out Provider<IProvider> provider)
        {
            Guard.NotNull(manager);

            provider = null;

            try
            {
                provider = manager.GetProvider(systemName, storeId);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
