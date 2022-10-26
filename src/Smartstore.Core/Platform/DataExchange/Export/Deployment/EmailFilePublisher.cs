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

        public EmailFilePublisher(SmartDbContext db, DatabaseMediaStorageProvider dbMediaStorageProvider)
        {
            _db = db;
            _dbMediaStorageProvider = dbMediaStorageProvider;
        }

        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancelToken)
        {
            var emailAddresses = deployment.EmailAddresses
                .SplitSafe(',')
                .Where(x => x.IsEmail())
                .ToArray();

            if (!emailAddresses.Any())
            {
                return;
            }

            var emailAccount = await _db.EmailAccounts.FindByIdAsync(deployment.EmailAccountId, false, cancelToken);
            var fromEmailAddress = emailAccount.ToMailAddress();
            var files = await context.GetDeploymentFilesAsync(cancelToken);
            var num = 0;

            foreach (var emailAddress in emailAddresses)
            {
                var queuedEmail = new QueuedEmail
                {
                    From = fromEmailAddress,
                    SendManually = false,
                    To = emailAddress,
                    Subject = deployment.EmailSubject.NaIfEmpty(),
                    Body = deployment.EmailSubject.NaIfEmpty(),
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
