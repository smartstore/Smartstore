using System.Collections.Generic;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// An event message, which will be published after customer has been exported by GDPR tool.
    /// </summary>
    public class CustomerExportedEvent
    {
        public CustomerExportedEvent(Customer customer, IDictionary<string, object> result)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(result, nameof(result));

            Customer = customer;
            Result = result;
        }

        public Customer Customer { get; private set; }
        public IDictionary<string, object> Result { get; private set; }
    }
}
