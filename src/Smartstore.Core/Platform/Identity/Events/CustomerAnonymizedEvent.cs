using Smartstore.Core.Localization;
using Smartstore.Events;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// An event message, which will be published after customer has been anonymized by GDPR tool.
    /// </summary>
    public class CustomerAnonymizedEvent : IEventMessage
    {
        public CustomerAnonymizedEvent(IGdprTool tool, Customer customer, Language language, bool pseudomyzeContent)
        {
            GdprTool = Guard.NotNull(tool);
            Customer = Guard.NotNull(customer);
            Language = Guard.NotNull(language);
            PseudomyzeContent = pseudomyzeContent;
        }

        public IGdprTool GdprTool { get; }

        public Customer Customer { get; }

        public Language Language { get; }

        public bool PseudomyzeContent { get; }
    }
}
