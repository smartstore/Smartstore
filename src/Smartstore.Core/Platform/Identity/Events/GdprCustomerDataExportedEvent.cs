namespace Smartstore.Core.Identity
{
    /// <summary>
    /// An event message that will be published after customer has been exported by GDPR tool.
    /// </summary>
    public class GdprCustomerDataExportedEvent
    {
        public GdprCustomerDataExportedEvent(Customer customer, IDictionary<string, object> result)
        {
            Customer = Guard.NotNull(customer, nameof(customer));
            Result = Guard.NotNull(result, nameof(result));
        }

        public Customer Customer { get; }
        public IDictionary<string, object> Result { get; }
    }
}
