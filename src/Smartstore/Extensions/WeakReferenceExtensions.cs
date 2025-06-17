#nullable enable

namespace Smartstore
{
    public static class WeakReferenceExtensions
    {
        /// <summary>
        /// Gets the target of the weak reference if it is still alive.
        /// </summary>
        public static T? GetTarget<T>(this WeakReference<T> weakReference) where T : class
        {
            if (weakReference.TryGetTarget(out var  target))
            {
                return target;
            }

            return null;
        }
    }
}
