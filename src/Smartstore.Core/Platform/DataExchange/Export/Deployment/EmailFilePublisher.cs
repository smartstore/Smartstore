using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Data;
using Smartstore.Core.Messaging;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public class EmailFilePublisher : IFilePublisher
    {
        private readonly SmartDbContext _db;
        private readonly DatabaseMediaStorageProvider _dbMediaStorageProvider;
        private readonly IExportProfileService _exportProfileService;

        public EmailFilePublisher(
            SmartDbContext db, 
            DatabaseMediaStorageProvider dbMediaStorageProvider,
            IExportProfileService exportProfileService)
        {
            _db = db;
            _dbMediaStorageProvider = dbMediaStorageProvider;
            _exportProfileService = exportProfileService;
        }

        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancelToken)
        {
            var emailAddresses = deployment.EmailAddresses
                .SplitSafe(',')
                .Where(x => x.IsEmail())
                .ToArray();

            if (emailAddresses.Length == 0)
            {
                return;
            }

            await _db.LoadReferenceAsync(deployment, x => x.Profile, false, null, cancelToken);

            var emailAccount = await _db.EmailAccounts.FindByIdAsync(deployment.EmailAccountId, false, cancelToken);
            var fromEmailAddress = emailAccount.ToMailAddress();
            // INFO: activate ExportProfile.CreateZipArchive if files in subfolders are also to be sent.
            var files = await context.GetDeploymentFilesAsync(false, cancelToken);
            var subject = _exportProfileService.ResolveTokens(deployment.Profile, deployment.EmailSubject).NaIfEmpty();
            var num = 0;

            foreach (var emailAddress in emailAddresses)
            {
                var queuedEmail = new QueuedEmail
                {
                    From = fromEmailAddress,
                    SendManually = false,
                    To = emailAddress,
                    Subject = subject,
                    Body = subject,
                    CreatedOnUtc = DateTime.UtcNow,
                    EmailAccountId = deployment.EmailAccountId
                };

                foreach (var file in files)
                {
                    var name = file.Name;
                    var attachment = new QueuedEmailAttachment
                    {
                        StorageLocation = EmailAttachmentStorageLocation.Blob,
                        Name = name,
                        MimeType = MimeTypes.MapNameToMimeType(name)
                    };

                    var item = MediaStorageItem.FromFile(file);
                    await _dbMediaStorageProvider.ApplyBlobAsync(attachment, item, false);

                    queuedEmail.Attachments.Add(attachment);
                }

                _db.QueuedEmails.Add(queuedEmail);

                // Blob data could be large, so better not bulk commit here.
                await _db.SaveChangesAsync(cancelToken);
                num++;
            }

            context.Log.Info($"{num} email(s) created and queued for deployment.");
        }
    }
}
