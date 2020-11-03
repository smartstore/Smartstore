using Microsoft.EntityFrameworkCore.Diagnostics;
using Smartstore.Data.Caching.Internal;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Entity Framework Core Second Level Caching Library
    /// </summary>
    public class EfCacheInterceptor : DbCommandInterceptor
    {
        private readonly EfCacheInterceptorProcessor _processor;

        /// <summary>
        /// Entity Framework Core Second Level Caching Library
        /// Please use
        /// services.AddDbContextPool&lt;ApplicationDbContext&gt;((serviceProvider, optionsBuilder) =&gt;
        ///                   optionsBuilder.UseSqlServer(...).AddInterceptors(serviceProvider.GetRequiredService&lt;EfCacheInterceptor&gt;()));
        /// to register it.
        /// </summary>
        public EfCacheInterceptor(EfCacheInterceptorProcessor processor)
        {
            _processor = processor;
        }

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteNonQuery
        /// </summary>
        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            if (eventData.Context == null)
            {
                return base.NonQueryExecuted(command, eventData, result);
            }

            return _processor.ProcessExecutedCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteNonQueryAsync.
        /// </summary>
        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<int>(_processor.ProcessExecutedCommandsAsync(command, eventData.Context, result));
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteNonQuery.
        /// </summary>
        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context == null)
            {
                return base.NonQueryExecuting(command, eventData, result);
            }

            return _processor.ProcessExecutingCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteNonQueryAsync.
        /// </summary>
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<InterceptionResult<int>>(_processor.ProcessExecutingCommands(command, eventData.Context, result));
        }

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteReader.
        /// </summary>
        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            if (eventData.Context == null)
            {
                return base.ReaderExecuted(command, eventData, result);
            }

            return _processor.ProcessExecutedCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteReaderAsync.
        /// </summary>
        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<DbDataReader>(_processor.ProcessExecutedCommandsAsync(command, eventData.Context, result));
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteReader.
        /// </summary>
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            if (eventData.Context == null)
            {
                return base.ReaderExecuting(command, eventData, result);
            }

            return _processor.ProcessExecutingCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteReaderAsync.
        /// </summary>
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<InterceptionResult<DbDataReader>>(_processor.ProcessExecutingCommands(command, eventData.Context, result));
        }

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteScalar.
        /// </summary>
        public override object ScalarExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            object result)
        {
            if (eventData.Context == null)
            {
                return base.ScalarExecuted(command, eventData, result);
            }

            return _processor.ProcessExecutedCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteScalarAsync.
        /// </summary>
        public override ValueTask<object> ScalarExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            object result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<object>(_processor.ProcessExecutedCommandsAsync(command, eventData.Context, result));
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteScalar.
        /// </summary>
        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            if (eventData.Context == null)
            {
                return base.ScalarExecuting(command, eventData, result);
            }

            return _processor.ProcessExecutingCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteScalarAsync.
        /// </summary>
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<InterceptionResult<object>>(_processor.ProcessExecutingCommands(command, eventData.Context, result));
        }
    }
}
