namespace Smartstore.Web.Bundling
{
    public interface IBundleContextAccessor
    {
        BundleContext BundleContext { get; set; }
    }

    public class BundleContextAccessor : IBundleContextAccessor
    {
        private static AsyncLocal<BundleContextHolder> _bundleContextCurrent = new();

        public BundleContext BundleContext
        {
            get
            {
                return _bundleContextCurrent.Value?.Context;
            }
            set
            {
                var holder = _bundleContextCurrent.Value;
                if (holder != null)
                {
                    // Clear current BundleContext trapped in the AsyncLocals, as its done.
                    holder.Context = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the BundleContext in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    _bundleContextCurrent.Value = new BundleContextHolder { Context = value };
                }
            }
        }

        class BundleContextHolder
        {
            public BundleContext Context;
        }
    }
}
