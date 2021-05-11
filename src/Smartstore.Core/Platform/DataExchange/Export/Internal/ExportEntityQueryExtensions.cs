using System;
using System.Linq;
using Smartstore.Domain;

namespace Smartstore.Core.DataExchange.Export.Internal
{
    internal static class ExportEntityQueryExtensions
    {
        public static IQueryable<TEntity> ApplyPagingForExport<TEntity>(this IQueryable<TEntity> query, int? skip, int take, DataExporterContext ctx)
            where TEntity : BaseEntity
        {
            var skipValue = skip.GetValueOrDefault();
            if (skipValue == 0 && ctx.LastId == 0)
            {
                skipValue = Math.Max(ctx.Request.Profile.Offset, 0);
            }

            query = query.OrderBy(x => x.Id);

            if (skipValue > 0)
            {
                query = query.Skip(skipValue);
            }
            else if (ctx.LastId > 0)
            {
                query = query.Where(x => x.Id > ctx.LastId);
            }

            if (take != int.MaxValue)
            {
                query = query.Take(take);
            }

            return query;
        }
    }
}
