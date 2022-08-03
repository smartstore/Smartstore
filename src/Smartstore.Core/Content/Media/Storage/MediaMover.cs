using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Data;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Content.Media.Storage
{
    public class MediaMover : IMediaMover
    {
        private const int PAGE_SIZE = 50;

        private readonly SmartDbContext _db;
        private readonly INotifier _notifier;
        private readonly ISettingService _settingService;

        public MediaMover(SmartDbContext db, INotifier notifier, ISettingService settingService)
        {
            _db = db;
            _notifier = notifier;
            _settingService = settingService;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual async Task<bool> MoveAsync(
            Provider<IMediaStorageProvider> sourceProvider,
            Provider<IMediaStorageProvider> targetProvider,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(sourceProvider, nameof(sourceProvider));
            Guard.NotNull(targetProvider, nameof(targetProvider));

            // Source must support sending
            if (sourceProvider.Value is not IMediaSender sender)
            {
                throw new ArgumentException(T("Admin.Media.StorageMovingNotSupported", sourceProvider.Metadata.SystemName));
            }

            // Target must support receiving
            if (targetProvider.Value is not IMediaReceiver receiver)
            {
                throw new ArgumentException(T("Admin.Media.StorageMovingNotSupported", targetProvider.Metadata.SystemName));
            }

            // Source and target provider must not be equal
            if (sender == receiver)
            {
                throw new ArgumentException(T("Admin.Media.CannotMoveToSameProvider"));
            }

            var success = false;
            var utcNow = DateTime.UtcNow;
            var context = new MediaMoverContext(sender, receiver);

            // We are about to process data in chunks but want to commit ALL at once after ALL chunks have been processed successfully.
            // AutoDetectChanges true required for newly inserted binary data.
            using (var scope = new DbContextScope(ctx: _db, autoDetectChanges: _db.DataProvider.CanStreamBlob ? false : null/*, retainConnection: true*/))
            {
                var query = _db.MediaFiles.AsQueryable();

                if (sender is DatabaseMediaStorageProvider && !_db.DataProvider.CanReadSequential)
                {
                    query = query.Include(x => x.MediaStorage);
                }

                using (var transaction = await _db.Database.BeginTransactionAsync(cancelToken))
                {
                    try
                    {
                        var pager = new FastPager<MediaFile>(query, PAGE_SIZE);
                        while ((await pager.ReadNextPageAsync<MediaFile>(cancelToken)).Out(out var files))
                        {
                            if (cancelToken.IsCancellationRequested)
                            {
                                break;
                            }

                            foreach (var file in files)
                            {
                                // Move item from source to target
                                await sender.MoveToAsync(receiver, context, file);

                                file.UpdatedOnUtc = utcNow;
                                ++context.MovedItems;
                            }

                            if (!cancelToken.IsCancellationRequested)
                            {
                                await _db.SaveChangesAsync(cancelToken);

                                // Detach all entities from previous page to save memory
                                scope.DbContext.DetachEntities(files, deep: true);
                            }
                        }

                        if (!cancelToken.IsCancellationRequested)
                        {
                            await transaction.CommitAsync(cancelToken);
                            success = true;
                        }
                        else
                        {
                            success = false;
                            await transaction.RollbackAsync(CancellationToken.None);
                        }
                    }
                    catch (Exception exception)
                    {
                        success = false;
                        await transaction.RollbackAsync(cancelToken);

                        _notifier.Error(exception);
                        Logger.Error(exception);
                    }
                }
            }


            if (success)
            {
                await _settingService.ApplySettingAsync("Media.Storage.Provider", targetProvider.Metadata.SystemName);
                await _db.SaveChangesAsync(cancelToken);
            }

            // Inform both provider about ending
            await sender.OnCompletedAsync(context, success, cancelToken);
            await receiver.OnCompletedAsync(context, success, cancelToken);

            return success;
        }
    }
}