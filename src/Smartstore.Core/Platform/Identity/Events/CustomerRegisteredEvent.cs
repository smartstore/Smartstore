namespace Smartstore.Core.Identity
{
    /// <summary>
    /// An event message, which will be published after customer has registered
    /// </summary>
    public class CustomerRegisteredEvent
    {
        public Customer Customer
        {
            get;
            set;
        }
    }
}
