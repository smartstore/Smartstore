using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Scheduling;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Media.Tasks;

/// <summary>
/// A task for deleting transient media from the database, i.e. files and downloads that have been uploaded but never assigned to an entity.
/// </summary>
/// <exception cref="DeleteTrackedFileException">Thrown if the file to be deleted is tracked.</exception>
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

        using var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Important);

        // First delete transient downloads so that they won't reference media files that are deleted in the next step.
        numDeleted += await _db.Downloads
            .Where(x => x.IsTransient && x.UpdatedOnUtc < olderThan)
            .ExecuteDeleteAsync(cancelToken);

        var files = await _db.MediaFiles
            .Where(x => x.IsTransient && x.UpdatedOnUtc < olderThan)
            .ToListAsync(cancelToken);

        foreach (var file in files)
        {
            await _mediaService.DeleteFileAsync(file, true);
            numDeleted += 1;
        }

        await _db.SaveChangesAsync(cancelToken);

        if (numDeleted > 1000 && _db.DataProvider.CanOptimizeTable)
        {
            await CommonHelper.TryAction(() => _db.DataProvider.OptimizeTableAsync<MediaFile>(cancelToken));
        }
    }
}