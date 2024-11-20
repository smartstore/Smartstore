namespace Smartstore.PayPal.Client.Messages
{
    public class ExceptionMessage
    {
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name;

        [JsonProperty("message", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Message;

        [JsonProperty("debug_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DebugId;

        [JsonProperty("details", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ExceptionDetails> Details;

        [JsonProperty("links", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<LinkDescription> Links;
    }

    public class ExceptionDetails
    {
        [JsonProperty("field")]
        public string Field;

        [JsonProperty("issue")]
        public string Issue;

        [JsonProperty("description")]
        public string Description;
    }
}
