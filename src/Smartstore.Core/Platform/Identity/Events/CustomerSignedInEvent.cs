using Smartstore.Events;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// An event message, which will be published after customer has logged in.
    /// </summary>
    public class CustomerSignedInEvent : IEventMessage
    {
        public Customer Customer { get; set; }
    }
}
