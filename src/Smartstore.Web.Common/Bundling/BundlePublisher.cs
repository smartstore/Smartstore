namespace Smartstore.Web.Bundling
{
    internal class BundlePublisher
    {
        public void RegisterBundles(IApplicationContext appContext, IBundleCollection bundles)
        {
            Guard.NotNull(appContext, nameof(appContext));
            Guard.NotNull(bundles, nameof(bundles));

            var bundleProviders = appContext.TypeScanner
                .FindTypes<IBundleProvider>()
                .Select(providerType => Activator.CreateInstance(providerType) as IBundleProvider)
                .OrderByDescending(provider => provider.Priority)
                .ToList();

            foreach (var provider in bundleProviders)
            {
                provider.RegisterBundles(appContext, bundles);
            }
        }
    }
}
