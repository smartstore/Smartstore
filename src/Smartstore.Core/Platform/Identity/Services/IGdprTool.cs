using Smartstore.Core.Localization;

namespace Smartstore.Core.Identity
{
    public enum IdentifierDataType
    {
        Text,
        LongText,
        Name,
        UserName,
        EmailAddress,
        Url,
        IpAddress,
        PhoneNumber,
        Address,
        PostalCode,
        DateTime
    }

    /// <summary>
    /// Contract for General Data Protection Regulation (GDPR) compliancy.
    /// </summary>
    public partial interface IGdprTool
    {
        /// <summary>
        /// Exports all data stored for a customer into a dictionary. Exported data contains all
        /// personal data, addresses, order history, reviews, forum posts, private messages etc.
        /// </summary>
        /// <param name="customer">The customer to export data for.</param>
        /// <returns>The exported data</returns>
        /// <remarks>This method fulfills the "GDPR Data Portability" requirement.</remarks>
        Task<IDictionary<string, object>> ExportCustomerAsync(Customer customer);

        /// <summary>
        /// Anonymizes a customer's (personal) data and saves the result.
        /// </summary>
        /// <param name="customer">The customer to anonymize.</param>
        /// <param name="pseudomyzeContent"></param>
        /// <remarks>This method fulfills the "GDPR Right to be forgotten" requirement.</remarks>
        Task AnonymizeCustomerAsync(Customer customer, bool pseudomyzeContent);

        /// <summary>
        /// Anonymizes a data piece. The caller is responsible for database commit.
        /// </summary>
        /// <param name="entity">The entity instance that contains the data.</param>
        /// <param name="expression">The expression to the property that holds the data.</param>
        /// <param name="type">The value kind.</param>
        /// <param name="language">Language for data masking</param>
        void AnonymizeData<TEntity>(TEntity entity, Expression<Func<TEntity, object>> expression, IdentifierDataType type, Language language = null) where TEntity : BaseEntity;
    }
}
