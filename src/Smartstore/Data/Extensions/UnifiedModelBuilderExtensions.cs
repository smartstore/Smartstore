#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Data;
using Smartstore.Data.Providers;

namespace Smartstore
{
    public static class UnifiedModelBuilderExtensions
    {
        /// <summary>
        /// Returns a value indicating whether the given include properties can be set.
        /// </summary>
        /// <param name="indexBuilder">The builder for the index being configured.</param>
        /// <param name="propertyNames">An array of property names to be used in 'include' clause.</param>
        /// <returns> <c>true</c> if the given include properties can be set. </returns>
        public static bool CanSetIncludeProperties(
            this IConventionIndexBuilder indexBuilder,
            IReadOnlyList<string>? propertyNames)
        {
            return GetFacade().CanSetIncludeProperties(indexBuilder, propertyNames);
        }

        /// <summary>
        /// Adds an INCLUDE clause to the index definition with property names from the specified expression.
        /// This clause specifies a list of columns which will be included as a non-key part in the index.
        /// </summary>
        /// <remarks>
        /// Depending on the underlying database provider, this method may or may not
        /// include index properties.
        /// </remarks>
        /// <param name="indexBuilder">The builder for the index being configured.</param>
        /// <param name="includeExpression">
        /// <para>
        /// A lambda expression representing the property(s) to be included in the INCLUDE clause
        /// (<c>blog => blog.Url</c>).
        /// </para>
        /// <para>
        /// If multiple properties are to be included then specify an anonymous type including the
        /// properties (<c>post => new { post.Title, post.BlogId }</c>).
        /// </para>
        /// </param>
        /// <returns>A builder to further configure the index.</returns>
        public static IndexBuilder<TEntity> IncludeProperties<TEntity>(
            this IndexBuilder<TEntity> indexBuilder,
            Expression<Func<TEntity, object>> includeExpression)
        {
            IncludeProperties((IndexBuilder)indexBuilder, includeExpression.GetPropertyAccessList().Select(x => x.Name).ToArray());
            return indexBuilder;
        }

        /// <summary>
        /// Adds an INCLUDE clause to the index definition with the specified property names.
        /// This clause specifies a list of columns which will be included as a non-key part in the index.
        /// </summary>
        /// <remarks>
        /// Depending on the underlying database provider, this method may or may not
        /// include index properties.
        /// </remarks>
        /// <param name="indexBuilder">The builder for the index being configured.</param>
        /// <param name="propertyNames">An array of property names to be used in INCLUDE clause.</param>
        /// <returns>A builder to further configure the index.</returns>
        public static IndexBuilder<TEntity> IncludeProperties<TEntity>(
            this IndexBuilder<TEntity> indexBuilder,
            params string[] propertyNames)
        {
            IncludeProperties((IndexBuilder)indexBuilder, propertyNames);
            return indexBuilder;
        }

        /// <summary>
        /// Adds an INCLUDE clause to the index definition with the specified property names.
        /// This clause specifies a list of columns which will be included as a non-key part in the index.
        /// </summary>
        /// <remarks>
        /// Depending on the underlying database provider, this method may or may not
        /// include index properties.
        /// </remarks>
        /// <param name="indexBuilder">The builder for the index being configured.</param>
        /// <param name="propertyNames">An array of property names to be used in INCLUDE clause.</param>
        /// <returns>A builder to further configure the index.</returns>
        public static IndexBuilder IncludeProperties(
            this IndexBuilder indexBuilder,
            params string[] propertyNames)
        {
            GetFacade().IncludeIndexProperties(indexBuilder, propertyNames);
            return indexBuilder;
        }

        /// <summary>
        /// Configures the model to use a sequence per hierarchy to generate values for key properties marked as <see cref="ValueGenerated.OnAdd" />.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="nameSuffix">The name that will suffix the table name for each sequence created automatically.</param>
        /// <param name="schema">The schema of the sequence.</param>
        /// <returns>The same builder instance so that multiple calls can be chained.</returns>
        public static ModelBuilder UseKeySequences(
            this ModelBuilder modelBuilder,
            string? nameSuffix = null,
            string? schema = null)
        {
            GetFacade().UseKeySequences(modelBuilder, nameSuffix, schema);
            return modelBuilder;
        }

        private static UnifiedModelBuilderFacade GetFacade()
        {
            return DataSettings.Instance.DbFactory.ModelBuilderFacade;
        }
    }
}
