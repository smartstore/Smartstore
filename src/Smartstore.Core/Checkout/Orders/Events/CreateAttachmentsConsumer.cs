using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Messaging.Events;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders.Events
{
    internal class CreateAttachmentsConsumer : IConsumer
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task HandleEventAsync(MessageQueuingEvent message,
            Lazy<PdfInvoiceHttpClient> client,
            PdfSettings pdfSettings)
        {
            var messageName = message.MessageContext.MessageTemplate.Name;
            var processMessage = (pdfSettings.AttachOrderPdfToOrderPlacedEmail && messageName.EqualsNoCase(MessageTemplateNames.OrderPlacedCustomer))
                || (pdfSettings.AttachOrderPdfToOrderCompletedEmail && messageName.EqualsNoCase(MessageTemplateNames.OrderCompletedCustomer));

            if (processMessage 
                && message.MessageModel.Get("Order") is IDictionary<string, object> order 
                && order.Get("ID") is int orderId)
            {
                try
                {
                    var result = await client.Value.GetPdfInvoiceAsync(orderId);

                    message.QueuedEmail.Attachments.Add(new()
                    {
                        StorageLocation = EmailAttachmentStorageLocation.Blob,
                        MimeType = result.MimeType,
                        Name = result.FileName,
                        MediaStorage = new MediaStorage { Data = result.Buffer }
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, T("Admin.System.QueuedEmails.ErrorCreatingAttachment"));
                }
            }
        }
    }
}