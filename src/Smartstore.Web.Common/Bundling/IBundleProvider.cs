namespace Smartstore.Web.Bundling
{
    public interface IBundleProvider
    {
        void RegisterBundles(IApplicationContext appContext, IBundleCollection bundles);

        int Priority { get; }
    }
}
