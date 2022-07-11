using Smartstore.Core.DataExchange.Import.Events;
using Smartstore.Core.Messaging;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.DataExchange.Import
{
    public class NewsletterSubscriptionImporter : IEntityImporter
    {
        private readonly ICommonServices _services;

        public NewsletterSubscriptionImporter(ICommonServices services)
        {
            _services = services;
        }

        public static string[] SupportedKeyFields => new[] { nameof(NewsletterSubscription.Email) };
        public static string[] DefaultKeyFields => new[] { nameof(NewsletterSubscription.Email) };

        public async Task ExecuteAsync(ImportExecuteContext context, CancellationToken cancelToken)
        {
            var currentStoreId = _services.StoreContext.CurrentStore.Id;
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<NewsletterSubscription>();

            using (var scope = new DbContextScope(_services.DbContext, autoDetectChanges: false, minHookImportance: HookImportance.Important, deferCommit: true))
            {
                await context.SetProgressAsync(segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows);

                foreach (var row in batch)
                {
                    try
                    {
                        NewsletterSubscription subscription = null;
                        var email = row.GetDataValue<string>(nameof(NewsletterSubscription.Email));
                        var storeId = row.GetDataValue<int>(nameof(NewsletterSubscription.StoreId));

                        if (storeId == 0)
                        {
                            storeId = currentStoreId;
                        }

                        if (row.HasDataValue(nameof(NewsletterSubscription.Active)) && row.TryGetDataValue(nameof(NewsletterSubscription.Active), out bool active))
                        {
                        }
                        else
                        {
                            active = true;  // Default.
                        }

                        if (email.IsEmpty())
                        {
                            context.Result.AddWarning("Skipped empty email address.", row.RowInfo, nameof(NewsletterSubscription.Email));
                            continue;
                        }

                        if (email.Length > 255)
                        {
                            context.Result.AddWarning($"Skipped email address '{email}'. It exceeds the maximum allowed length of 255.", row.RowInfo, nameof(NewsletterSubscription.Email));
                            continue;
                        }

                        if (!email.IsEmail())
                        {
                            context.Result.AddWarning($"Skipped invalid email address '{email}'.", row.RowInfo, nameof(NewsletterSubscription.Email));
                            continue;
                        }

                        foreach (var keyName in context.KeyFieldNames)
                        {
                            switch (keyName)
                            {
                                case nameof(NewsletterSubscription.Email):
                                    subscription = await _services.DbContext.NewsletterSubscriptions
                                        .OrderBy(x => x.Id)
                                        .FirstOrDefaultAsync(x => x.Email == email && x.StoreId == storeId, cancelToken);
                                    break;
                            }

                            if (subscription != null)
                                break;
                        }

                        if (subscription == null)
                        {
                            if (context.UpdateOnly)
                            {
                                ++context.Result.SkippedRecords;
                                continue;
                            }

                            subscription = new NewsletterSubscription
                            {
                                Active = active,
                                CreatedOnUtc = context.UtcNow,
                                Email = email,
                                NewsletterSubscriptionGuid = Guid.NewGuid(),
                                StoreId = storeId
                            };

                            _services.DbContext.NewsletterSubscriptions.Add(subscription);
                            context.Result.NewRecords++;
                        }
                        else
                        {
                            subscription.Active = active;
                            context.Result.ModifiedRecords++;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex.ToAllMessages(), row.RowInfo);
                    }
                }

                await scope.CommitAsync(cancelToken);
            }

            await _services.EventPublisher.PublishAsync(new ImportBatchExecutedEvent<NewsletterSubscription>(context, batch), cancelToken);
        }
    }
}
