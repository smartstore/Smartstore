using System.Diagnostics;
using System.Runtime.Serialization;

namespace Smartstore.Core.OutputCache
{
    [Serializable]
    [DebuggerDisplay("{CacheKey}, Url: {Url}, Query: {QueryString}, Duration: {Duration}, Tags: {Tags}")]
    public class OutputCacheItem : ICloneable<OutputCacheItem>
    {
        // used for serialization compatibility
        public static readonly string Version = "2";

        public string CacheKey { get; set; }
        public string RouteKey { get; set; }
        public DateTime CachedOnUtc { get; set; }
        public string Url { get; set; }
        public string QueryString { get; set; }
        public int Duration { get; set; }
        public string[] Tags { get; set; }

        public string Theme { get; set; }
        public int StoreId { get; set; }
        public int LanguageId { get; set; }
        public int CurrencyId { get; set; }
        public string CustomerRoles { get; set; }

        public string ContentType { get; set; }
        public int? ContentLength { get; set; }

        [IgnoreDataMember]
        public string Content { get; set; }

        [IgnoreDataMember]
        public DateTime ExpiresOnUtc => CachedOnUtc.AddSeconds(Duration);

        public bool IsValid(DateTime utcNow)
        {
            return utcNow < ExpiresOnUtc;
        }

        public string JoinTags()
        {
            if (Tags == null || Tags.Length == 0)
                return string.Empty;

            return string.Join(';', Tags);
        }

        public OutputCacheItem Clone() => (OutputCacheItem)MemberwiseClone();
        object ICloneable.Clone() => MemberwiseClone();
    }
}