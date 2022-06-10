using Smartstore.Core.Rules.Filters;

namespace Smartstore.Core.Logging
{
    public static class LogQueryExtensions
    {
        public static IQueryable<Log> ApplyDateFilter(this IQueryable<Log> query, DateTime? fromUtc, DateTime? toUtc)
        {
            Guard.NotNull(query, nameof(query));

            if (fromUtc.HasValue)
                query = query.Where(x => fromUtc.Value <= x.CreatedOnUtc);

            if (toUtc.HasValue)
                query = query.Where(x => toUtc.Value >= x.CreatedOnUtc);

            return query;
        }

        public static IQueryable<Log> ApplyLevelFilter(this IQueryable<Log> query, LogLevel? level)
        {
            Guard.NotNull(query, nameof(query));

            if (level.HasValue)
            {
                int logLevelId = (int)level.Value;
                return query.Where(x => x.LogLevelId == logLevelId);
            }

            return query;
        }

        public static IQueryable<Log> ApplyLoggerFilter(this IQueryable<Log> query, string logger)
        {
            Guard.NotNull(query, nameof(query));

            if (logger.HasValue())
            {
                return query.ApplySearchFilterFor(x => x.Logger, logger);
            }

            return query;
        }

        public static IQueryable<Log> ApplyMessageFilter(this IQueryable<Log> query, string message, bool includeException = true)
        {
            Guard.NotNull(query, nameof(query));

            if (message.HasValue())
            {
                return includeException
                    ? query.ApplySearchFilter(message, Rules.LogicalRuleOperator.Or, x => x.ShortMessage, x => x.FullMessage)
                    : query.ApplySearchFilterFor(x => x.ShortMessage, message);
            }

            return query;
        }
    }
}