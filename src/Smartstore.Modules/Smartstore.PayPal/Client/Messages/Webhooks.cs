namespace Smartstore.PayPal.Client.Messages
{
    public class Webhooks
    {
        /// <summary>
        /// An array of webhooks.
        /// </summary>
        [JsonProperty("webhooks", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Webhook[] Hooks { get; set; }
    }

    public class Webhook
    {
        /// <summary>
        /// The API caller-provided external ID. Used to reconcile API caller-initiated transactions with PayPal transactions. Appears in transaction and settlement reports.
        /// </summary>
        [JsonProperty("event_types", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public EventType[] EventTypes { get; set; }

        /// <summary>
        /// The Internet accessible URL configured to listen for incoming POST notification messages containing event information. 
        /// </summary>
        [JsonProperty("url", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Url { get; set; }

        /// <summary>
        /// The ID of the webhook.
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// An array of request-related HATEOAS links.
        /// </summary>
        [JsonProperty("links", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<LinkDescription> Links;
    }

    public class EventType
    {
        /// <summary>
        /// The unique event name.
        /// </summary>
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// A human-readable description of the event.
        /// </summary>
        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Description { get; set; }

        /// <summary>
        /// A human-readable description of the event.
        /// </summary>
        [JsonProperty("resource_versions", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] ResourceVersions { get; set; }

        /// <summary>
        /// A human-readable description of the event.
        /// </summary>
        [JsonProperty("status", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Status { get; set; }
    }
}
