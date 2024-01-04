using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Messaging;
using Smartstore.Net.Mail;
using Smartstore.Utilities;

namespace Smartstore.Core.Tests.Platform.Messaging
{
    [TestFixture]
    public class QueuedEmailServiceTests : ServiceTestBase
    {
        IMailService _mailService;
        QueuedEmailService _queuedEmailService;
        IMediaService _mediaService;
        IMediaUrlGenerator _mediaUrlGenerator;
        EmailAccountSettings _emailAccountSettings;
        Mock<IMediaService> _mediaServiceMock;

        [SetUp]
        public new void SetUp()
        {
            _emailAccountSettings = new EmailAccountSettings
            {
                DefaultEmailAccountId = 1,
                MailSendingDelay = 20,
                PickupDirectoryLocation = string.Empty
            };

            _mediaServiceMock = new Mock<IMediaService>();
            _mediaService = _mediaServiceMock.Object;

            var mediaStorageProvider = ProviderManager.GetProvider<IMediaStorageProvider>(DatabaseMediaStorageProvider.SystemName);

            _mediaServiceMock.Setup(x => x.StorageProvider).Returns(mediaStorageProvider.Value);

            var mediaUrlGeneratorMock = new Mock<IMediaUrlGenerator>();
            _mediaUrlGenerator = mediaUrlGeneratorMock.Object;

            var mailServiceMock = new Mock<IMailService>();
            _mailService = mailServiceMock.Object;

            _queuedEmailService = new QueuedEmailService(DbContext, _mailService, _mediaService, _emailAccountSettings);
        }

        [Test]
        public async Task Can_convert_email()
        {
            var qe = new QueuedEmail
            {
                Bcc = "bcc1@mail.com;bcc2@mail.com",
                Body = "Body",
                CC = "cc1@mail.com;cc2@mail.com",
                CreatedOnUtc = DateTime.UtcNow,
                From = "FromName <from@mail.com>",
                Priority = 10,
                ReplyTo = "ReplyToName <replyto@mail.com>",
                Subject = "Subject",
                To = "ToName <to@mail.com>"
            };

            // load attachment file resource and save as file
            var asm = typeof(QueuedEmailServiceTests).Assembly;
            var pdfStream = asm.GetManifestResourceStream($"{asm.GetName().Name}.Platform.Messaging.Attachment.pdf");
            var pdfBinary = pdfStream.ToByteArray();
            pdfStream.Seek(0, SeekOrigin.Begin);

            var path1 = "~/Attachment.pdf";
            var path2 = CommonHelper.MapPath(path1, false);
            Assert.That(await pdfStream.CopyToFileAsync(path2), Is.True);

            var attachBlob = new QueuedEmailAttachment
            {
                StorageLocation = EmailAttachmentStorageLocation.Blob,
                MediaStorage = new MediaStorage { Id = 1, Data = pdfBinary },
                MediaStorageId = 1,
                Name = "blob.pdf",
                MimeType = "application/pdf"
            };
            var attachPath1 = new QueuedEmailAttachment
            {
                StorageLocation = EmailAttachmentStorageLocation.Path,
                Path = path1,
                Name = "path1.pdf",
                MimeType = "application/pdf"
            };
            var attachPath2 = new QueuedEmailAttachment
            {
                StorageLocation = EmailAttachmentStorageLocation.Path,
                Path = path2,
                Name = "path2.pdf",
                MimeType = "application/pdf"
            };

            var fileReferenceFile = new MediaFile
            {
                Id = 1,
                MimeType = "application/pdf",
                MediaStorage = new MediaStorage { Id = 2, Data = pdfBinary },
                MediaStorageId = 2,
                Extension = ".pdf",
                Name = "file.pdf"
            };
            var attachFile = new QueuedEmailAttachment
            {
                Id = 1,
                StorageLocation = EmailAttachmentStorageLocation.FileReference,
                Name = "file.pdf",
                MimeType = "application/pdf",
                MediaFile = fileReferenceFile
            };

            qe.Attachments.Add(attachBlob);
            qe.Attachments.Add(attachFile);
            qe.Attachments.Add(attachPath1);
            qe.Attachments.Add(attachPath2);

            _mediaServiceMock.Setup(x => x.ConvertMediaFile(fileReferenceFile)).Returns(
                new MediaFileInfo(fileReferenceFile, _mediaService, _mediaUrlGenerator, string.Empty));

            using (var msg = _queuedEmailService.ConvertMail(qe))
            {
                Assert.That(msg, Is.Not.Null);
                Assert.That(msg.To, Is.Not.Null);
                Assert.That(msg.From, Is.Not.Null);

                Assert.That(msg.ReplyTo, Has.Count.EqualTo(1));

                var replyToAddress = new MailAddress("replyto@mail.com", "ReplyToName");
                Assert.That(msg.ReplyTo.First().ToString(), Is.EqualTo(replyToAddress.ToString()));

                Assert.That(msg.Cc, Has.Count.EqualTo(2));
                Assert.That(msg.Cc.First().Address, Is.EqualTo("cc1@mail.com"));
                Assert.That(msg.Cc.ElementAt(1).Address, Is.EqualTo("cc2@mail.com"));

                Assert.That(msg.Bcc, Has.Count.EqualTo(2));
                Assert.That(msg.Bcc.First().Address, Is.EqualTo("bcc1@mail.com"));
                Assert.That(msg.Bcc.ElementAt(1).Address, Is.EqualTo("bcc2@mail.com"));

                Assert.That(msg.Subject, Is.EqualTo(qe.Subject));
                Assert.That(msg.Body, Is.EqualTo(qe.Body));

                Assert.That(msg.Attachments, Has.Count.EqualTo(4));

                var attach1 = msg.Attachments.First();
                var attach2 = msg.Attachments.ElementAt(1);
                var attach3 = msg.Attachments.ElementAt(2);
                var attach4 = msg.Attachments.ElementAt(3);

                // test file names
                Assert.That(attach1.Name, Is.EqualTo("blob.pdf"));
                Assert.That(attach2.Name, Is.EqualTo("file.pdf"));
                Assert.That(attach3.Name, Is.EqualTo("path1.pdf"));
                Assert.That(attach4.Name, Is.EqualTo("path2.pdf"));

                // test file streams
                Assert.That(pdfBinary, Has.Length.EqualTo(attach1.ContentStream.Length));
                Assert.That(pdfBinary, Has.Length.EqualTo(attach2.ContentStream.Length));
                Assert.That(attach3.ContentStream.Length, Is.GreaterThan(0));
                Assert.That(attach4.ContentStream.Length, Is.GreaterThan(0));
            }

            // delete attachment file
            File.Delete(path2);
        }
    }
}