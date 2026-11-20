using NUnit.Framework;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Messaging;

namespace Smartstore.Core.Tests.Platform.Messaging;

[TestFixture]
public class QueuedEmailAttachmentTests
{
    [TestCase(EmailAttachmentStorageLocation.Blob)]
    [TestCase(EmailAttachmentStorageLocation.Path)]
    [TestCase(EmailAttachmentStorageLocation.FileReference)]
    public void Can_clone_email_attachment(EmailAttachmentStorageLocation storageLocation)
    {
        var attachment = new QueuedEmailAttachment
        {
            Id = 42,
            QueuedEmailId = 24,
            StorageLocation = storageLocation,
            IsPending = true,
            Path = storageLocation == EmailAttachmentStorageLocation.Path ? "~/attachment.pdf" : null,
            MediaFileId = storageLocation == EmailAttachmentStorageLocation.FileReference ? 11 : null,
            Name = "attachment.pdf",
            MimeType = "application/pdf",
            IsEmbedded = true,
            ContentId = "attachment-content",
            MediaStorageId = storageLocation == EmailAttachmentStorageLocation.Blob ? 12 : null,
            MediaStorage = storageLocation == EmailAttachmentStorageLocation.Blob
                ? new MediaStorage { Id = 12, Data = [1, 2, 3] }
                : null
        };

        var clone = attachment.Clone();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(clone, Is.Not.SameAs(attachment));
            Assert.That(clone.Id, Is.Zero);
            Assert.That(clone.QueuedEmailId, Is.Zero);
            Assert.That(clone.StorageLocation, Is.EqualTo(storageLocation));
            Assert.That(clone.IsPending, Is.True);
            Assert.That(clone.Path, Is.EqualTo(attachment.Path));
            Assert.That(clone.MediaFileId, Is.EqualTo(attachment.MediaFileId));
            Assert.That(clone.Name, Is.EqualTo(attachment.Name));
            Assert.That(clone.MimeType, Is.EqualTo(attachment.MimeType));
            Assert.That(clone.IsEmbedded, Is.True);
            Assert.That(clone.ContentId, Is.EqualTo(attachment.ContentId));
            Assert.That(clone.MediaStorageId, Is.Null);

            if (storageLocation == EmailAttachmentStorageLocation.Blob)
            {
                Assert.That(clone.MediaStorage, Is.Not.SameAs(attachment.MediaStorage));
                Assert.That(clone.MediaStorage.Id, Is.Zero);
                Assert.That(clone.MediaStorage.Data, Is.EqualTo(attachment.MediaStorage.Data));
                Assert.That(clone.MediaStorage.Data, Is.Not.SameAs(attachment.MediaStorage.Data));
            }
            else
            {
                Assert.That(clone.MediaStorage, Is.Null);
            }
        }
    }
}
