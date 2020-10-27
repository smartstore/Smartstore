using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Domain;

namespace Smartstore.Data.Batching
{
    public static class IQueryableBatchExtensions
    {
        /// <example>
        /// context.Items.Where(a => a.ItemId >  500).BatchDelete();
        /// </example>
        public static int BatchDelete(this IQueryable query)
        {
            var context = query.GetDbContext();
            (string sql, var parameters) = BatchUtil.GetSqlDelete(query, context);

            return context.Database.ExecuteSqlRaw(sql, parameters);
        }

        /// <example>
        /// context.Items.Where(x => x.ItemId <= 500).BatchUpdate(new Item { Quantity = x.Quantity + 100 }, nameof(Item.Quantity));
        /// </example>
        public static int BatchUpdate(this IQueryable query, object updateValues, params string[] updateColumns)
        {
            var context = query.GetDbContext();
            var (sql, parameters) = BatchUtil.GetSqlUpdate(query, context, updateValues, updateColumns.ToList());
            return context.Database.ExecuteSqlRaw(sql, parameters);
        }

        /// <example>
        /// context.Items.Where(x => x.ItemId <= 500).BatchUpdate(x => new Item { Quantity = x.Quantity + 100 });
        /// </example>
        public static int BatchUpdate<T>(this IQueryable<T> query, Expression<Func<T, T>> updateExpression) 
            where T : BaseEntity, new()
        {
            var context = query.GetDbContext();
            var (sql, parameters) = BatchUtil.GetSqlUpdate(query, context, updateExpression);
            return context.Database.ExecuteSqlRaw(sql, parameters);
        }

        public static int BatchUpdate(this IQueryable query, Type type, Expression<Func<object, object>> updateExpression)
        {
            var context = query.GetDbContext();
            var (sql, parameters) = BatchUtil.GetSqlUpdate(query, context, type, updateExpression);
            return context.Database.ExecuteSqlRaw(sql, parameters);
        }

        #region Async

        /// <example>
        /// context.Items.Where(a => a.ItemId >  500).BatchDeleteAsync();
        /// </example>
        public static async Task<int> BatchDeleteAsync(this IQueryable query, CancellationToken cancellationToken = default)
        {
            var context = query.GetDbContext();
            (string sql, var parameters) = BatchUtil.GetSqlDelete(query, context);
            return await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
        }

        /// <example>
        /// context.Items.Where(x => x.ItemId <= 500).BatchUpdateAsync(new Item { Quantity = x.Quantity + 100 }, nameof(Item.Quantity));
        /// </example>
        public static async Task<int> BatchUpdateAsync(this IQueryable query, 
            object updateValues, 
            IEnumerable<string> updateColumns = null, 
            CancellationToken cancellationToken = default)
        {
            var context = query.GetDbContext();
            var (sql, parameters) = BatchUtil.GetSqlUpdate(query, context, updateValues, updateColumns?.ToList());
            return await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
        }

        /// <example>
        /// context.Items.Where(x => x.ItemId <= 500).BatchUpdateAsync(x => new Item { Quantity = x.Quantity + 100 });
        /// </example>
        public static async Task<int> BatchUpdateAsync<T>(this IQueryable<T> query, 
            Expression<Func<T, T>> updateExpression, 
            CancellationToken cancellationToken = default)
            where T : BaseEntity, new()
        {
            var context = query.GetDbContext();
            var (sql, parameters) = BatchUtil.GetSqlUpdate(query, context, updateExpression);
            return await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<int> BatchUpdateAsync(this IQueryable query, 
            Type type, 
            Expression<Func<object, object>> updateExpression, 
            CancellationToken cancellationToken = default)
        {
            var context = query.GetDbContext();
            var (sql, parameters) = BatchUtil.GetSqlUpdate(query, context, type, updateExpression);
            return await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}