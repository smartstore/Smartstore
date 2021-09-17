using System.Collections.Generic;

namespace Smartstore.Core.Identity
{
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
