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
            Assert.IsTrue(await pdfStream.CopyToFileAsync(path2));

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
                Assert.IsNotNull(msg);
                Assert.IsNotNull(msg.To);
                Assert.IsNotNull(msg.From);

                Assert.AreEqual(msg.ReplyTo.Count, 1);

                var replyToAddress = new MailAddress("replyto@mail.com", "ReplyToName");
                Assert.AreEqual(replyToAddress.ToString(), msg.ReplyTo.First().ToString());

                Assert.AreEqual(msg.Cc.Count, 2);
                Assert.AreEqual(msg.Cc.First().Address, "cc1@mail.com");
                Assert.AreEqual(msg.Cc.ElementAt(1).Address, "cc2@mail.com");

                Assert.AreEqual(msg.Bcc.Count, 2);
                Assert.AreEqual(msg.Bcc.First().Address, "bcc1@mail.com");
                Assert.AreEqual(msg.Bcc.ElementAt(1).Address, "bcc2@mail.com");

                Assert.AreEqual(qe.Subject, msg.Subject);
                Assert.AreEqual(qe.Body, msg.Body);

                Assert.AreEqual(4, msg.Attachments.Count);

                var attach1 = msg.Attachments.First();
                var attach2 = msg.Attachments.ElementAt(1);
                var attach3 = msg.Attachments.ElementAt(2);
                var attach4 = msg.Attachments.ElementAt(3);

                // test file names
                Assert.AreEqual(attach1.Name, "blob.pdf");
                Assert.AreEqual(attach2.Name, "file.pdf");
                Assert.AreEqual(attach3.Name, "path1.pdf");
                Assert.AreEqual(attach4.Name, "path2.pdf");

                // test file streams
                Assert.AreEqual(attach1.ContentStream.Length, pdfBinary.Length);
                Assert.AreEqual(attach2.ContentStream.Length, pdfBinary.Length);
                Assert.Greater(attach3.ContentStream.Length, 0);
                Assert.Greater(attach4.ContentStream.Length, 0);
            }

            // delete attachment file
            File.Delete(path2);
        }
    }
}