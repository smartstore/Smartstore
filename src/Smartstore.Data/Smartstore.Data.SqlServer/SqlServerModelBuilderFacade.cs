#nullable enable

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Data.Providers;

namespace Smartstore.Data.SqlServer
{
    internal class SqlServerModelBuilderFacade : UnifiedModelBuilderFacade
    {
        public override bool CanSetIncludeProperties(IConventionIndexBuilder indexBuilder, IReadOnlyList<string>? propertyNames)
        {
            return SqlServerIndexBuilderExtensions.CanSetIncludeProperties(indexBuilder, propertyNames, false);
        }

        public override void IncludeIndexProperties(IndexBuilder indexBuilder, params string[] propertyNames)
        {
            SqlServerIndexBuilderExtensions.IncludeProperties(indexBuilder, propertyNames);
        }

        public override void UseKeySequences(ModelBuilder modelBuilder, string? nameSuffix = null, string? schema = null)
        {
            SqlServerModelBuilderExtensions.UseKeySequences(modelBuilder, nameSuffix, schema);
        }
    }
}
