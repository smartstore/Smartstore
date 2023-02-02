#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smartstore.Data.Providers
{
    /// <summary>
    /// Provides a facade for unified model building across different storage providers.
    /// </summary>
    public class UnifiedModelBuilderFacade
    {
        public virtual bool CanSetIncludeProperties(
            IConventionIndexBuilder indexBuilder, 
            IReadOnlyList<string>? propertyNames)
            => false;

        public virtual void IncludeIndexProperties(
            IndexBuilder indexBuilder,
            params string[] propertyNames)
        {
            // Noop by default
        }

        public virtual void UseKeySequences(
            ModelBuilder modelBuilder,
            string? nameSuffix = null,
            string? schema = null)
        {
            // Noop by default
        }
    }
}
