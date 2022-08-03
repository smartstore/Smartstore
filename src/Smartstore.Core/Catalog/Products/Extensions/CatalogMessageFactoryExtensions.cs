using Smartstore.Core.Identity;
using Smartstore.Core.Messaging;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class CatalogMessageFactoryExtensions
    {
        /// <summary>
        /// Sends an "email a friend" message.
        /// </summary>
        /// <param name="factory">Message factory.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="product">Product.</param>
        /// <param name="fromEmail">Sender email address.</param>
        /// <param name="toEmail">Recipient email address.</param>
        /// <param name="personalMessage">Message text.</param>
        /// <param name="languageId">Language identifier.</param>
        /// <returns>Create message result.</returns>
        public static Task<CreateMessageResult> SendShareProductMessageAsync(
            this IMessageFactory factory,
            Customer customer,
            Product product,
            string fromEmail,
            string toEmail,
            string personalMessage,
            int languageId = 0)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(product, nameof(product));

            var model = new NamedModelPart("Message")
            {
                ["Body"] = personalMessage.NullEmpty(),
                ["From"] = fromEmail.NullEmpty(),
                ["To"] = toEmail.NullEmpty()
            };

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.ShareProduct, languageId, customer: customer),
                true,
                product,
                model);
        }

        /// <summary>
        /// Sends an ask product question message.
        /// </summary>
        /// <param name="factory">Message factory.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="product">Product.</param>
        /// <param name="senderEmail">Sender email address.</param>
        /// <param name="senderName">Sender name.</param>
        /// <param name="senderPhone">Sender phone number.</param>
        /// <param name="question">Question text.</param>
        /// <param name="attributeInfo">Attribute informations.</param>
        /// <param name="productUrl">Product URL.</param>
        /// <param name="isQuoteRequest">A value indicating whether the message is a quote request.</param>
        /// <param name="languageId">Language identifier.</param>
        /// <returns>Create message result.</returns>
        public static Task<CreateMessageResult> SendProductQuestionMessageAsync(
            this IMessageFactory factory,
            Customer customer,
            Product product,
            string senderEmail,
            string senderName,
            string senderPhone,
            string question,
            string attributeInfo,
            string productUrl,
            bool isQuoteRequest,
            int languageId = 0)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(product, nameof(product));

            var model = new NamedModelPart("Message")
            {
                ["ProductUrl"] = productUrl.NullEmpty(),
                ["IsQuoteRequest"] = isQuoteRequest,
                ["ProductAttributes"] = attributeInfo.NullEmpty(),
                ["Message"] = question.NullEmpty(),
                ["SenderEmail"] = senderEmail.NullEmpty(),
                ["SenderName"] = senderName.NullEmpty(),
                ["SenderPhone"] = senderPhone.NullEmpty()
            };

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.ProductQuestion, languageId, customer: customer),
                true,
                product,
                model);
        }

        /// <summary>
        /// Sends a product review notification message to a store owner.
        /// </summary>
        /// <param name="factory">Message factory.</param>
        /// <param name="productReview">Product review</param>
        /// <param name="languageId">Language identifier.</param>
        /// <returns>Create message result.</returns>
        public static Task<CreateMessageResult> SendProductReviewNotificationMessageAsync(this IMessageFactory factory, ProductReview productReview, int languageId = 0)
        {
            Guard.NotNull(productReview, nameof(productReview));

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.ProductReviewStoreOwner, languageId, customer: productReview.Customer),
                true,
                productReview,
                productReview.Product);
        }

        /// <summary>
        /// Sends a "quantity below" notification to a store owner.
        /// </summary>
        /// <param name="factory">Message factory.</param>
        /// <param name="product">Product.</param>
        /// <param name="languageId">Language identifier.</param>
        /// <returns>Create message result.</returns>
        public static Task<CreateMessageResult> SendQuantityBelowStoreOwnerNotificationAsync(this IMessageFactory factory, Product product, int languageId = 0)
        {
            Guard.NotNull(product, nameof(product));

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.QuantityBelowStoreOwner, languageId),
                true,
                product);
        }

        /// <summary>
        /// Sends a 'Back in stock' notification message to a customer.
        /// </summary>
        /// <param name="factory">Message factory.</param>
        /// <param name="subscription">Back in stock subscription.</param>
        /// <returns>Create message result.</returns>
        public static Task<CreateMessageResult> SendBackInStockNotificationAsync(this IMessageFactory factory, BackInStockSubscription subscription)
        {
            Guard.NotNull(subscription, nameof(subscription));

            var customer = subscription.Customer;
            var languageId = customer.GenericAttributes.LanguageId ?? 0;

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.BackInStockCustomer, languageId, subscription.StoreId, customer),
                true,
                subscription.Product);
        }
    }
}
