using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Batching;

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
            var numDeleted = await _db.Logs.CountAsync();
            
            using var tx = await _db.Database.BeginTransactionAsync(cancelToken);

            await _db.DataProvider.TruncateTableAsync<Log>();
            await _db.DataProvider.SetTableIncrementAsync<Log>(0);

            await tx.CommitAsync(cancelToken);

            return numDeleted;
        }

        public virtual async Task<int> ClearLogsAsync(DateTime maxAgeUtc, LogLevel maxLevel, CancellationToken cancelToken = default)
        {
            var numDeleted = await _db.Logs
                .Where(x => x.CreatedOnUtc <= maxAgeUtc && x.LogLevelId < (int)maxLevel)
                .BatchDeleteAsync(cancelToken);

            if (numDeleted > 100)
            {
                await _db.DataProvider.ShrinkDatabaseAsync(cancelToken);
            }

            return numDeleted;
        }
    }
}