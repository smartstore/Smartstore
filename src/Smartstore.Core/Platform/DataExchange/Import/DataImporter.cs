using System.Security;
using Autofac;
using Smartstore.Collections;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.DataExchange.Csv;
using Smartstore.Core.DataExchange.Import.Events;
using Smartstore.Core.DataExchange.Import.Internal;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.IO;
using Smartstore.Net.Mail;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class DataImporter : IDataImporter
    {
        private readonly ICommonServices _services;
        private readonly ILifetimeScopeAccessor _scopeAccessor;
        private readonly IImportProfileService _importProfileService;
        private readonly ILanguageService _languageService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IMailService _mailService;
        private readonly ContactDataSettings _contactDataSettings;
        private readonly DataExchangeSettings _dataExchangeSettings;

        public DataImporter(
            ICommonServices services,
            ILifetimeScopeAccessor scopeAccessor,
            IImportProfileService importProfileService,
            ILanguageService languageService,
            IEmailAccountService emailAccountService,
            IMailService mailService,
            ContactDataSettings contactDataSettings,
            DataExchangeSettings dataExchangeSettings)
        {
            _services = services;
            _scopeAccessor = scopeAccessor;
            _importProfileService = importProfileService;
            _languageService = languageService;
            _emailAccountService = emailAccountService;
            _mailService = mailService;
            _contactDataSettings = contactDataSettings;
            _dataExchangeSettings = dataExchangeSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task ImportAsync(DataImportRequest request, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request);
            Guard.NotNull(cancelToken);

            var profile = await _services.DbContext.ImportProfiles.FindByIdAsync(request.ProfileId, false, cancelToken);
            if (!(profile?.Enabled ?? false))
                return;

            var (ctx, logFile) = await CreateImporterContext(request, profile, cancelToken);
            using var logger = new TraceLogger(logFile, false);
            ctx.Log = ctx.ExecuteContext.Log = logger;

            try
            {
                await CheckPermission(ctx);

                var context = ctx.ExecuteContext;
                var files = await _importProfileService.GetImportFilesAsync(profile, profile.ImportRelatedData);
                var fileGroups = files.ToMultimap(x => x.RelatedType?.ToString() ?? string.Empty, x => x);

                logger.Info(CreateLogHeader(profile, fileGroups));
                await _services.EventPublisher.PublishAsync(new ImportExecutingEvent(context), cancelToken);

                foreach (var fileGroup in fileGroups)
                {
                    context.Result = ctx.Results[fileGroup.Key] = new();

                    foreach (var file in fileGroup.Value)
                    {
                        if (context.Abort == DataExchangeAbortion.Hard)
                            break;

                        if (!file.File.Exists)
                            throw new FileNotFoundException($"File does not exist {file.File.SubPath}.");

                        try
                        {
                            var csvConfiguration = file.IsCsv
                                ? (new CsvConfigurationConverter().ConvertFrom<CsvConfiguration>(profile.FileTypeConfiguration) ?? CsvConfiguration.ExcelFriendlyConfiguration)
                                : CsvConfiguration.ExcelFriendlyConfiguration;

                            using var stream = await file.File.OpenReadAsync(cancelToken);

                            context.File = file;
                            context.ColumnMap = file.RelatedType.HasValue ? new ColumnMap() : ctx.ColumnMap;
                            context.DataTable = LightweightDataTable.FromFile(
                                file.File.Name,
                                stream,
                                stream.Length,
                                csvConfiguration,
                                profile.Skip,
                                profile.Take > 0 ? profile.Take : int.MaxValue);

                            var segmenter = new ImportDataSegmenter(context.DataTable, context.ColumnMap);

                            context.DataSegmenter = segmenter;
                            context.Result.TotalRecords = segmenter.TotalRows;

                            while (context.Abort == DataExchangeAbortion.None && segmenter.ReadNextBatch())
                            {
                                using var batchScope = _scopeAccessor.LifetimeScope.BeginLifetimeScope();

                                // Apply changes made by TaskContextVirtualizer.VirtualizeAsync (e.g. required for checking permissions).
                                batchScope.Resolve<IWorkContext>().CurrentCustomer = _services.WorkContext.CurrentCustomer;
                                batchScope.Resolve<IStoreContext>().CurrentStore = _services.StoreContext.CurrentStore;

                                // It would be nice if we could make all dependencies use our TraceLogger.
                                var importerFactory = batchScope.Resolve<Func<ImportEntityType, IEntityImporter>>();
                                var importer = importerFactory(profile.EntityType);

                                await importer.ExecuteAsync(context, cancelToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            context.Abort = DataExchangeAbortion.Hard;
                            context.Result.AddError(ex, $"The importer failed: {ex.ToAllMessages()}.");
                        }
                        finally
                        {
                            context.Result.EndDateUtc = DateTime.UtcNow;

                            if (context.IsMaxFailures)
                                context.Result.AddWarning("Import aborted. The maximum number of failures has been reached.");

                            if (ctx.CancelToken.IsCancellationRequested)
                                context.Result.AddWarning("Import aborted. A cancellation has been requested.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorsAll(ex);
                ctx.ExecuteContext.Result.AddError(ex);
            }
            finally
            {
                await Finalize(ctx);
            }

            cancelToken.ThrowIfCancellationRequested();
        }

        private async Task Finalize(DataImporterContext ctx)
        {
            if (ctx == null)
                return;

            try
            {
                await _services.EventPublisher.PublishAsync(new ImportExecutedEvent(ctx.ExecuteContext), ctx.CancelToken);
            }
            catch (Exception ex)
            {
                ctx.Log?.ErrorsAll(ex);
            }

            var profile = await _services.DbContext.ImportProfiles.FindByIdAsync(ctx.Request.ProfileId, true, ctx.CancelToken);

            try
            {
                await SendCompletionEmail(profile, ctx);
            }
            catch (Exception ex)
            {
                ctx.Log?.ErrorsAll(ex);
            }

            try
            {
                LogResults(profile, ctx);
            }
            catch (Exception ex)
            {
                ctx.Log?.ErrorsAll(ex);
            }

            try
            {
                if (ctx.Results.TryGetValue(string.Empty, out var result))
                {
                    profile.ResultInfo = XmlHelper.Serialize(result.Clone());

                    await _services.DbContext.SaveChangesAsync(ctx.CancelToken);
                }
            }
            catch (Exception ex)
            {
                ctx.Log?.ErrorsAll(ex);
            }

            if (ctx.ExecuteContext?.ClearCache ?? false)
            {
                try
                {
                    await _services.Cache.ClearAsync();
                }
                catch (Exception ex)
                {
                    ctx.Log?.ErrorsAll(ex);
                }
            }

            try
            {
                ctx.Request.CustomData.Clear();
                ctx.Results.Clear();
            }
            catch (Exception ex)
            {
                ctx.Log?.ErrorsAll(ex);
            }
        }

        #region Utilities

        private async Task SendCompletionEmail(ImportProfile profile, DataImporterContext ctx)
        {
            var emailAccount = _emailAccountService.GetDefaultEmailAccount();
            if (emailAccount == null || emailAccount.Host.IsEmpty())
            {
                return;
            }

            var result = ctx.ExecuteContext.Result;

            if (_dataExchangeSettings.ImportCompletionEmail == DataExchangeCompletionEmail.Never ||
                (_dataExchangeSettings.ImportCompletionEmail == DataExchangeCompletionEmail.OnError && !result.HasErrors))
            {
                return;
            }

            var store = _services.StoreContext.CurrentStore;
            var storeInfo = $"{store.Name} ({store.GetBaseUrl()})";
            using var psb = StringBuilderPool.Instance.Get(out var body);

            body.Append(T("Admin.DataExchange.Import.CompletedEmail.Body", storeInfo));

            if (result.LastError.HasValue())
            {
                body.AppendFormat("<p style=\"color: #B94A48;\">{0}</p>", result.LastError);
            }

            body.Append("<p>");

            body.AppendFormat("<div>{0}: {1} &middot; {2}: {3}</div>",
                T("Admin.Common.TotalRows"), result.TotalRecords,
                T("Admin.Common.Skipped"), result.SkippedRecords);

            body.AppendFormat("<div>{0}: {1} &middot; {2}: {3}</div>",
                T("Admin.Common.NewRecords"), result.NewRecords,
                T("Admin.Common.Updated"), result.ModifiedRecords);

            body.AppendFormat("<div>{0}: {1} &middot; {2}: {3}</div>",
                T("Admin.Common.Errors"), result.Errors,
                T("Admin.Common.Warnings"), result.Warnings);

            body.Append("</p>");

            using var message = new MailMessage
            {
                From = new(emailAccount.Email, emailAccount.DisplayName),
                Subject = T("Admin.DataExchange.Import.CompletedEmail.Subject").Value.FormatInvariant(profile.Name),
                Body = body.ToString()
            };

            if (_contactDataSettings.WebmasterEmailAddress.HasValue())
            {
                message.To.Add(new(_contactDataSettings.WebmasterEmailAddress));
            }

            if (message.To.Count == 0 && _contactDataSettings.CompanyEmailAddress.HasValue())
            {
                message.To.Add(new(_contactDataSettings.CompanyEmailAddress));
            }

            if (message.To.Count == 0)
            {
                message.To.Add(new(emailAccount.Email, emailAccount.DisplayName));
            }

            await using var client = await _mailService.ConnectAsync(emailAccount);
            await client.SendAsync(message, ctx.CancelToken);

            //_db.QueuedEmails.Add(new QueuedEmail
            //{
            //    From = emailAccount.Email,
            //    To = message.To.First().Address,
            //    Subject = message.Subject,
            //    Body = message.Body,
            //    CreatedOnUtc = DateTime.UtcNow,
            //    EmailAccountId = emailAccount.Id,
            //    SendManually = true
            //});
            //await _db.SaveChangesAsync();
        }

        private async Task<(DataImporterContext Context, IFile LogFile)> CreateImporterContext(DataImportRequest request, ImportProfile profile, CancellationToken cancelToken)
        {
            var dir = await _importProfileService.GetImportDirectoryAsync(profile, "Content", true);

            var executeContext = new ImportExecuteContext(T("Admin.DataExchange.Import.ProgressInfo"), cancelToken)
            {
                Request = request,
                ImportEntityType = profile.EntityType,
                ProgressCallback = request.ProgressCallback,
                UpdateOnly = profile.UpdateOnly,
                KeyFieldNames = profile.KeyFieldNames.SplitSafe(',').ToArray(),
                ImportDirectory = dir,
                ImageDownloadDirectory = await _importProfileService.GetImportDirectoryAsync(profile, @"Content\DownloadedImages", true),
                ExtraData = XmlHelper.Deserialize<ImportExtraData>(profile.ExtraData),
                Languages = await _languageService.GetAllLanguagesAsync(true),
                Stores = _services.StoreContext.GetAllStores().AsReadOnly()
            };

            // Relative paths for images always refer to the profile directory, not to its "Content" sub-directory.
            executeContext.ImageDirectory = _dataExchangeSettings.ImageImportFolder.HasValue()
                ? await _importProfileService.GetImportDirectoryAsync(profile, _dataExchangeSettings.ImageImportFolder, false)
                : dir.Parent;

            var context = new DataImporterContext
            {
                Request = request,
                CancelToken = cancelToken,
                ColumnMap = new ColumnMapConverter().ConvertFrom<ColumnMap>(profile.ColumnMapping) ?? new ColumnMap(),
                ExecuteContext = executeContext
            };

            var logFile = await dir.Parent.GetFileAsync("log.txt");

            return (context, logFile);
        }

        private string CreateLogHeader(ImportProfile profile, Multimap<string, ImportFile> files)
        {
            var executingCustomer = _services.WorkContext.CurrentCustomer;

            using var psb = StringBuilderPool.Instance.Get(out var sb);

            sb.AppendLine();
            sb.AppendLine(new string('-', 40));
            sb.AppendLine("Smartstore: v." + SmartstoreVersion.CurrentFullVersion);
            sb.Append("Import profile: " + profile.Name);
            sb.AppendLine(profile.Id == 0 ? " (transient)" : $" (ID {profile.Id})");

            foreach (var fileGroup in files)
            {
                var entityName = fileGroup.Key.NullEmpty() ?? profile.EntityType.ToString();
                var fileNames = string.Join(", ", fileGroup.Value.Select(x => x.File.Name));
                sb.AppendLine($"{entityName} files: {fileNames}");
            }

            sb.Append("Executed by: " + (executingCustomer.Email.HasValue() ? executingCustomer.Email : executingCustomer.SystemName));

            return sb.ToString();
        }

        private static void LogResults(ImportProfile profile, DataImporterContext ctx)
        {
            using var psb = StringBuilderPool.Instance.Get(out var sb);

            foreach (var item in ctx.Results)
            {
                var result = item.Value;
                var entityName = item.Key.HasValue() ? item.Key : profile.EntityType.ToString();

                sb.Clear();
                sb.AppendLine();
                sb.AppendLine(new string('-', 40));
                sb.AppendLine("Object:         " + entityName);
                sb.AppendLine("Started:        " + result.StartDateUtc.ToLocalTime());
                sb.AppendLine("Finished:       " + result.EndDateUtc.ToLocalTime());
                sb.AppendLine("Duration:       " + (result.EndDateUtc - result.StartDateUtc).ToString("g"));
                sb.AppendLine("Rows total:     " + result.TotalRecords);
                sb.AppendLine("Rows processed: " + result.AffectedRecords);
                sb.AppendLine("Rows imported:  " + result.NewRecords);
                sb.AppendLine("Rows updated:   " + result.ModifiedRecords);
                sb.AppendLine("Warnings:       " + result.Warnings);
                sb.Append("Errors:         " + result.Errors);
                ctx.Log.Info(sb.ToString());

                foreach (var message in result.Messages)
                {
                    if (message.MessageType == ImportMessageType.Error)
                    {
                        ctx.Log.Error(new Exception(message.FullMessage), message.ToString());
                    }
                    else if (message.MessageType == ImportMessageType.Warning)
                    {
                        ctx.Log.Warn(message.ToString());
                    }
                    else
                    {
                        ctx.Log.Info(message.ToString());
                    }
                }
            }
        }

        private async Task CheckPermission(DataImporterContext ctx)
        {
            if (ctx.Request.HasPermission)
            {
                return;
            }

            var customer = _services.WorkContext.CurrentCustomer;

            if (customer.IsBackgroundTaskAccount())
            {
                return;
            }

            if (!await _services.Permissions.AuthorizeAsync(Permissions.Configuration.Import.Execute, customer))
            {
                throw new SecurityException(await _services.Permissions.GetUnauthorizedMessageAsync(Permissions.Configuration.Import.Execute));
            }
        }

        #endregion
    }
}
