using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Data;
using Smartstore.Utilities;

namespace Smartstore.Core.Logging
{
    public partial class DbLogService : IDbLogService
    {
        private readonly SmartDbContext _db;
        private readonly CommonSettings _commonSettings;

        public DbLogService(
            SmartDbContext db,
            CommonSettings commonSettings)
        {
            _db = db;
            _commonSettings = commonSettings;
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
                .ExecuteDeleteAsync(_commonSettings.DeleteBulkSize, cancelToken);

            var dataProvider = _db.DataProvider;
            if (numDeleted > 100 && dataProvider.CanOptimizeTable)
            {
                var tableName = _db.Model.FindEntityType(typeof(Log)).GetTableName();
                await CommonHelper.TryAction(() => dataProvider.OptimizeTableAsync(tableName, cancelToken));
            }

            return numDeleted;
        }
    }
}