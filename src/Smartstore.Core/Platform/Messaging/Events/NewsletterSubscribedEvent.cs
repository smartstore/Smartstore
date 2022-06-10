namespace Smartstore.Core.Messaging.Events
{
    public class NewsletterSubscribedEvent : IEquatable<NewsletterSubscribedEvent>
    {
        public NewsletterSubscribedEvent(string email)
        {
            Guard.NotEmpty(email, nameof(email));

            Email = email;
        }

        public string Email { get; init; }

        public override bool Equals(object obj)
        {
            return Equals(obj as NewsletterSubscribedEvent);
        }

        public bool Equals(NewsletterSubscribedEvent other)
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
