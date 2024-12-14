namespace Smartstore.PayPal.Client.Messages
{
    public class Patch<T>
    {
        /// <summary>
        /// REQUIRED.
        /// The operation.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string Op;

        /// <summary>
        /// The <a href="https://tools.ietf.org/html/rfc6901">JSON Pointer</a> to the target document location at which to complete the operation.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string Path;

        /// <summary>
        /// The value to apply. The <code>remove</code> operation does not require a value.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public T Value;
    }
}
