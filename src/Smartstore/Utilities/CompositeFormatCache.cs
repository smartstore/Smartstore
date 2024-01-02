using System.Collections.Concurrent;

namespace System.Text
{
    /// <summary>
    /// A singleton memory cache for <see cref="CompositeFormat"/> instances.
    /// </summary>
    public static class CompositeFormatCache
    {
        private readonly static ConcurrentDictionary<string, CompositeFormat> _cache = new();

        /// <summary>
        /// Gets or creates a <see cref="CompositeFormat"/> instance for the given <paramref name="format"/>.
        /// </summary>
        /// <param name="format">The format string to create a <see cref="CompositeFormat"/> for.</param>
        public static CompositeFormat Get(string format)
        {
            return _cache.GetOrAdd(format, CompositeFormat.Parse);
        }
    }
}
