using Smartstore.Events;

namespace Smartstore.Core.Messaging.Events
{
    public class NewsletterUnsubscribedEvent : IEventMessage, IEquatable<NewsletterUnsubscribedEvent>
    {
        public NewsletterUnsubscribedEvent(string email)
        {
            Guard.NotEmpty(email);

            Email = email;
        }

        public string Email { get; private set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as NewsletterUnsubscribedEvent);
        }

        public bool Equals(NewsletterUnsubscribedEvent other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Equals(other.Email, Email);
        }

        public override int GetHashCode()
        {
            return (Email != null ? Email.GetHashCode() : 0);
        }
    }
}
