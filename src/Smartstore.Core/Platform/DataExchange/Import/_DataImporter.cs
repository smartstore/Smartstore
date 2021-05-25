using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smartstore.Collections;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.DataExchange.Csv;
using Smartstore.Core.DataExchange.Import.Events;
using Smartstore.Core.DataExchange.Import.Internal;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Domain;
using Smartstore.Net.Mail;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class DataImporter : IDataImporter
    {
        private readonly ICommonServices _services;
        private readonly Func<ImportEntityType, IEntityImporter> _importerFactory;
        private readonly ILanguageService _languageService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IMailService _mailService;
        private readonly DataExchangeSettings _dataExchangeSettings;
        private readonly ContactDataSettings _contactDataSettings;

        public DataImporter(
            ICommonServices services,
            Func<ImportEntityType, IEntityImporter> importerFactory,
            ILanguageService languageService,
            IEmailAccountService emailAccountService,
            IMailService mailService,
            DataExchangeSettings dataExchangeSettings,
            ContactDataSettings contactDataSettings)
        {
            _services = services;
            _importerFactory = importerFactory;
            _languageService = languageService;
            _emailAccountService = emailAccountService;
            _mailService = mailService;
            _dataExchangeSettings = dataExchangeSettings;
            _contactDataSettings = contactDataSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task ImportAsync(DataImportRequest request, CancellationToken cancellationToken)
        {
            Guard.NotNull(request, nameof(request));
            Guard.NotNull(request.Profile, nameof(request.Profile));
            Guard.NotNull(cancellationToken, nameof(cancellationToken));

            var profile = request.Profile;
            if (!profile.Enabled)
            {
                return;
            }

            var ctx = await CreateImporterContext(request, cancellationToken);

            try
            {
                // TODO: (mg) (core) get import files.
                Multimap<RelatedEntityType?, ImportFile> files = null;
                var context = ctx.ExecuteContext;

                if (!await HasPermission(ctx))
                {
                    throw new SmartException("You do not have permission to perform the selected import.");
                }

                _services.MediaService.ImagePostProcessingEnabled = false;

                ctx.Log.Info(CreateLogHeader(files, ctx));

                await _services.EventPublisher.PublishAsync(new ImportExecutingEvent(context), cancellationToken);

                foreach (var fileGroup in files)
                {
                    context.Result = ctx.Results[fileGroup.Key?.ToString()?.EmptyNull()] = new ImportResult();

                    foreach (var file in fileGroup.Value)
                    {
                        if (context.Abort == DataExchangeAbortion.Hard)
                            break;

                        if (!file.File.Exists)
                            throw new SmartException($"File does not exist {file.File.SubPath}.");

                        var csvConfiguration = file.IsCsv
                            ? (new CsvConfigurationConverter().ConvertFrom<CsvConfiguration>(profile.FileTypeConfiguration) ?? CsvConfiguration.ExcelFriendlyConfiguration)
                            : CsvConfiguration.ExcelFriendlyConfiguration;

                        using var stream = file.File.OpenRead();

                        context.DataTable = LightweightDataTable.FromFile(
                            file.File.Name,
                            stream,
                            stream.Length,
                            csvConfiguration,
                            profile.Skip,
                            profile.Take > 0 ? profile.Take : int.MaxValue);

                        context.ColumnMap = file.RelatedType.HasValue ? new ColumnMap() : ctx.ColumnMap;
                        context.File = file;

                        try
                        {
                            await ctx.Importer.ExecuteAsync(context);
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

                            if (ctx.CancellationToken.IsCancellationRequested)
                                context.Result.AddWarning("Import aborted. A cancellation has been requested.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ctx?.Log?.ErrorsAll(ex);
            }
            finally
            {
                await Finalize(ctx);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task Finalize(DataImporterContext ctx)
        {
            try
            {
                _services.MediaService.ImagePostProcessingEnabled = true;

                await _services.EventPublisher.PublishAsync(new ImportExecutedEvent(ctx.ExecuteContext), ctx.CancellationToken);
            }
            catch (Exception ex)
            {
                ctx?.Log?.ErrorsAll(ex);
            }

            try
            {
                // Database context sharing problem: if there are entities in modified state left by the provider due to SaveChangesAsync failure,
                // then all subsequent SaveChanges would fail too (e.g. profile update below or ITaskStore.UpdateTaskAsync...).
                // So whatever it is, detach\dispose all what the tracker still has tracked.

                _services.DbContext.DetachEntities<BaseEntity>();
            }
            catch (Exception ex)
            {
                ctx?.Log?.ErrorsAll(ex);
            }

            try
            {
                await SendCompletionEmail(ctx);
            }
            catch (Exception ex)
            {
                ctx?.Log?.ErrorsAll(ex);
            }

            try
            {
                LogResults(ctx);
            }
            catch (Exception ex)
            {
                ctx?.Log?.ErrorsAll(ex);
            }

            try
            {
                if (ctx.Results.TryGetValue(string.Empty, out var result))
                {
                    ctx.Request.Profile.ResultInfo = XmlHelper.Serialize(result.Clone());

                    await _services.DbContext.SaveChangesAsync(ctx.CancellationToken);
                }
            }
            catch (Exception ex)
            {
                ctx?.Log?.ErrorsAll(ex);
            }

            try
            {
                ctx.Request.CustomData.Clear();
                ctx.Results.Clear();
                ctx.Log = null;
            }
            catch (Exception ex)
            {
                ctx?.Log?.ErrorsAll(ex);
            }
        }

        #region Utilities

        private async Task SendCompletionEmail(DataImporterContext ctx)
        {
            var emailAccount = _emailAccountService.GetDefaultEmailAccount();
            var result = ctx.ExecuteContext.Result;
            var store = _services.StoreContext.CurrentStore;
            var storeInfo = $"{store.Name} ({store.Url})";
            var intro = _services.Localization.GetResource("Admin.DataExchange.Import.CompletedEmail.Body").FormatInvariant(storeInfo);

            using var psb = StringBuilderPool.Instance.Get(out var body);
            body.Append(intro);

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

            var message = new MailMessage
            {
                From = new(emailAccount.Email, emailAccount.DisplayName),
                Subject = T("Admin.DataExchange.Import.CompletedEmail.Subject").Value.FormatInvariant(ctx.Request.Profile.Name),
                Body = body.ToString()
            };

            if (_contactDataSettings.WebmasterEmailAddress.HasValue())
            {
                message.To.Add(new(_contactDataSettings.WebmasterEmailAddress));
            }

            if (!message.To.Any() && _contactDataSettings.CompanyEmailAddress.HasValue())
            {
                message.To.Add(new(_contactDataSettings.CompanyEmailAddress));
            }

            if (!message.To.Any())
            {
                message.To.Add(new(emailAccount.Email, emailAccount.DisplayName));
            }

            await using var client = await _mailService.ConnectAsync(emailAccount);
            await client.SendAsync(message, ctx.CancellationToken);

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

        private async Task<DataImporterContext> CreateImporterContext(DataImportRequest request, CancellationToken cancellationToken)
        {
            var profile = request.Profile;

            // TODO: (mg) (core) setup file logger for data import.
            ILogger logger = null;

            var executeContext = new ImportExecuteContext(T("Admin.DataExchange.Import.ProgressInfo"), cancellationToken)
            {
                Request = request,
                DataExchangeSettings = _dataExchangeSettings,
                Services = _services,
                Log = logger,
                Languages = await _languageService.GetAllLanguagesAsync(true),
                UpdateOnly = profile.UpdateOnly,
                KeyFieldNames = profile.KeyFieldNames.SplitSafe(",").ToArray(),
                // TODO: (mg) (core) get import folder for import.
                //ImportFolder = profile.GetImportFolder()
                ExtraData = XmlHelper.Deserialize<ImportExtraData>(profile.ExtraData)
            };

            var context = new DataImporterContext
            {
                Request = request,
                CancellationToken = cancellationToken,
                Log = logger,
                Importer = _importerFactory(profile.EntityType),
                ColumnMap = new ColumnMapConverter().ConvertFrom<ColumnMap>(profile.ColumnMapping) ?? new ColumnMap(),
                ExecuteContext = executeContext
            };

            return context;
        }

        private string CreateLogHeader(Multimap<RelatedEntityType?, ImportFile> files, DataImporterContext ctx)
        {
            var executingCustomer = _services.WorkContext.CurrentCustomer;
            var profile = ctx.Request.Profile;

            using var psb = StringBuilderPool.Instance.Get(out var sb);

            sb.AppendLine();
            sb.AppendLine(new string('-', 40));
            sb.AppendLine("Smartstore: v." + SmartstoreVersion.CurrentFullVersion);
            sb.AppendLine("Import profile: " + profile.Name);
            sb.AppendLine(profile.Id == 0 ? " (transient)" : $" (ID {profile.Id})");

            foreach (var fileGroup in files)
            {
                var entityName = fileGroup.Key.HasValue ? fileGroup.Key.Value.ToString() : profile.EntityType.ToString();
                var fileNames = string.Join(", ", fileGroup.Value.Select(x => x.File.Name));
                sb.AppendLine($"{entityName} files: {fileNames}");
            }

            sb.Append("Executed by: " + (executingCustomer.Email.HasValue() ? executingCustomer.Email : executingCustomer.SystemName));

            return sb.ToString();
        }

        private static void LogResults(DataImporterContext ctx)
        {
            using var psb = StringBuilderPool.Instance.Get(out var sb);

            foreach (var item in ctx.Results)
            {
                var result = item.Value;
                var entityName = item.Key.HasValue() ? item.Key : ctx.Request.Profile.EntityType.ToString();

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

        private async Task<bool> HasPermission(DataImporterContext ctx)
        {
            if (ctx.Request.HasPermission)
            {
                return true;
            }

            var customer = _services.WorkContext.CurrentCustomer;

            if (customer.SystemName == SystemCustomerNames.BackgroundTask)
            {
                return true;
            }

            return await _services.Permissions.AuthorizeAsync(Permissions.Configuration.Import.Execute);
        }

        #endregion
    }
}
