using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Scheduling;

namespace Smartstore.Core.Content.Media.Tasks
{
    /// <summary>
    /// Represents a task for deleting transient media from the database
	/// (files and downloads which have been uploaded but never assigned to an entity).
    /// </summary>
    public partial class TransientMediaClearTask : ITask
    {
        private readonly SmartDbContext _db;
        private readonly IMediaService _mediaService;

        public TransientMediaClearTask(SmartDbContext db, IMediaService mediaService)
        {
            _db = db;
            _mediaService = mediaService;
        }

        public virtual async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            cancelToken.ThrowIfCancellationRequested();

            // Delete all media records which are in transient state since at least 3 hours.
            var olderThan = DateTime.UtcNow.AddHours(-3);
            var numDeleted = 0;

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Important))
            {
                var files = await _db.MediaFiles.Where(x => x.IsTransient && x.UpdatedOnUtc < olderThan).ToListAsync(cancelToken);
                foreach (var file in files)
                {
                    await _mediaService.DeleteFileAsync(file, true);
                    numDeleted += 1;
                }

                await _db.SaveChangesAsync(cancelToken);

                numDeleted += await _db.Downloads
                    .Where(x => x.IsTransient && x.UpdatedOnUtc < olderThan)
                    .ExecuteDeleteAsync(cancelToken);

                if (numDeleted > 0 && _db.DataProvider.CanShrink)
                {
                    try
                    {
                        await _db.DataProvider.ShrinkDatabaseAsync(cancelToken);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
