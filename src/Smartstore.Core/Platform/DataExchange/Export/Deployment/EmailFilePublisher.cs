using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Messaging;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public class EmailFilePublisher : IFilePublisher
    {
        private readonly SmartDbContext _db;

        public EmailFilePublisher(SmartDbContext db)
        {
            _db = db;
        }

        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancellationToken)
        {
            var emailAddresses = deployment.EmailAddresses
                .SplitSafe(",")
                .Where(x => x.IsEmail())
                .ToArray();

            if (!emailAddresses.Any())
            {
                return;
            }

            var emailAccount = await _db.EmailAccounts.FindByIdAsync(deployment.EmailAccountId, false, cancellationToken);
            var fromEmailAddress = emailAccount.ToMailAddress();
            var files = await context.GetDeploymentFilesAsync(cancellationToken);
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
                    // TODO: (mg) (core) (perf) Use BlobStreams to save files to MediaStorage. See DatabaseMediaStorageProvider for usage (SaveFast).

                    var name = file.Name;
                    var bytes = await file.ReadAllBytesAsync();

                    queuedEmail.Attachments.Add(new QueuedEmailAttachment
                    {
                        StorageLocation = EmailAttachmentStorageLocation.Blob,
                        MediaStorage = new MediaStorage { Data = bytes },
                        Name = name,
                        MimeType = MimeTypes.MapNameToMimeType(name)
                    });
                }

                _db.QueuedEmails.Add(queuedEmail);

                // Blob data could be large, so better not bulk commit here.
                num += await _db.SaveChangesAsync(cancellationToken);
            }

            context.Log.Info($"{num} email(s) created and queued for deployment.");
        }
    }
}
