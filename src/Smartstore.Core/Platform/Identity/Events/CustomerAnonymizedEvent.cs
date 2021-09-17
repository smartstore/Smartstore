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
        public CustomerAnonymizedEvent(IGdprTool tool, Customer customer, Language language)
        {
            GdprTool = Guard.NotNull(tool, nameof(tool));
            Customer = Guard.NotNull(customer, nameof(customer));
            Language = Guard.NotNull(language, nameof(language));
        }

        public IGdprTool GdprTool { get; }

        public Customer Customer { get; }

        public Language Language { get; }
    }
}
