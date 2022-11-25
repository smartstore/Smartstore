using Smartstore.Core.Data;

namespace Smartstore.Core.Logging
{
    public partial class DbLogService : IDbLogService
    {
        private readonly SmartDbContext _db;

        public DbLogService(SmartDbContext db)
        {
            _db = db;
        }

        public virtual async Task<int> ClearLogsAsync(CancellationToken cancelToken = default)
        {
            var numDeleted = await _db.Logs.CountAsync(cancelToken);

            using var tx = await _db.Database.BeginTransactionAsync(cancelToken);

            await _db.DataProvider.TruncateTableAsync<Log>();
            await _db.DataProvider.SetTableIncrementAsync<Log>(1);

            await tx.CommitAsync(cancelToken);

            return numDeleted;
        }

        public virtual async Task<int> ClearLogsAsync(DateTime maxAgeUtc, LogLevel maxLevel, CancellationToken cancelToken = default)
        {
            var numDeleted = await _db.Logs
                .Where(x => x.CreatedOnUtc <= maxAgeUtc && x.LogLevelId < (int)maxLevel)
                .ExecuteDeleteAsync(cancelToken);

            if (numDeleted > 100 && _db.DataProvider.CanShrink)
            {
                await _db.DataProvider.ShrinkDatabaseAsync(cancelToken);
            }

            return numDeleted;
        }
    }
}