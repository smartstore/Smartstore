namespace Smartstore.PayPal.Client.Messages
{
    public class Webhooks
    {
        /// <summary>
        /// An array of webhooks.
        /// </summary>
        [JsonProperty("webhooks")]
        public Webhook[] Hooks { get; set; }
    }

    public class Webhook
    {
        /// <summary>
        /// The API caller-provided external ID. Used to reconcile API caller-initiated transactions with PayPal transactions. Appears in transaction and settlement reports.
        /// </summary>
        public EventType[] EventTypes { get; set; }

        /// <summary>
        /// The Internet accessible URL configured to listen for incoming POST notification messages containing event information. 
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The ID of the webhook.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// An array of request-related HATEOAS links.
        /// </summary>
        public List<LinkDescription> Links;
    }

    public class EventType
    {
        /// <summary>
        /// The unique event name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A human-readable description of the event.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// A human-readable description of the event.
        /// </summary>
        public string[] ResourceVersions { get; set; }

        /// <summary>
        /// A human-readable description of the event.
        /// </summary>
        public string Status { get; set; }
    }
}
