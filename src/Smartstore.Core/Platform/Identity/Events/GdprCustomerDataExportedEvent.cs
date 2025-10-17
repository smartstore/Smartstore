using Smartstore.Events;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// An event message that will be published after customer has been exported by GDPR tool.
    /// </summary>
    public class GdprCustomerDataExportedEvent : IEventMessage
    {
        public GdprCustomerDataExportedEvent(Customer customer, IDictionary<string, object> result)
        {
            Customer = Guard.NotNull(customer);
            Result = Guard.NotNull(result);
        }

        public Customer Customer { get; }
        public IDictionary<string, object> Result { get; }
    }
}
