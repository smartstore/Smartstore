using System.Collections.Generic;
using Newtonsoft.Json;

namespace Smartstore.PayPal.Client.Messages
{
    public class Event<T>
    {
        [JsonProperty("create_time", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CreateTime;

        [JsonProperty("event_type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string EventType;

        [JsonProperty("event_version", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string EventVersion;

        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id;

        [JsonProperty("links", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<LinkDescriptionObject> Links;

        [JsonProperty("resource", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public T Resource;

        [JsonProperty("resource_type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ResourceType;

        [JsonProperty("summary", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Summary;
    }

    public class LinkDescriptionObject
    {
        [JsonProperty("href", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Href;

        [JsonProperty("method", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Method;

        [JsonProperty("rel", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Rel;
    }
}
