namespace Smartstore.PayPal.Client.Messages
{
    public class WebhookEvent<T>
    {
        public string CreateTime;
        public string EventType;
        public string EventVersion;
        public string Id;
        public List<LinkDescription> Links;
        public T Resource;
        public string ResourceType;
        public string Summary;
    }

    public class LinkDescription
    {
        public string Href;
        public string Method;
        public string Rel;
    }
}
