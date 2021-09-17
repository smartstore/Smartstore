using System;
using System.Linq.Expressions;
using Smartstore.Core.Localization;
using Smartstore.Domain;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// An event message, which will be published after customer has been anonymized by GDPR tool.
    /// </summary>
    public class CustomerAnonymizedEvent
    {
        private readonly IGdprTool _gdprTool;

        public CustomerAnonymizedEvent(Customer customer, Language language, IGdprTool gdprTool)
        {
            Guard.NotNull(customer, nameof(customer));

            Customer = customer;
            Language = language;
            _gdprTool = gdprTool;
        }

        public Customer Customer { get; private set; }

        public Language Language { get; private set; }

        /// <summary>
        /// Anonymizes a part of the customer entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity to anonymize.</typeparam>
		/// <param name="entity">The entity instance that contains the data.</param>
		/// <param name="expression">The expression to the property that holds the data.</param>
		/// <param name="type">The value kind.</param>
		/// <param name="language">Language for data masking.</param>
        public void AnonymizeData<TEntity>(TEntity entity, Expression<Func<TEntity, object>> expression, IdentifierDataType type, Language language = null)
            where TEntity : BaseEntity
        {
            _gdprTool.AnonymizeData(entity, expression, type, language);
        }
    }
}
