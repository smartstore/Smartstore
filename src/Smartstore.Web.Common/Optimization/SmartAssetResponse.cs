using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using WebOptimizer;

namespace Smartstore.Web.Optimization
{
    public class SmartAssetResponse : IAssetResponse
    {
        private byte[] _body;
        private string _contentHash;
        
        public SmartAssetResponse()
        {
        }

        public SmartAssetResponse(byte[] body, string cacheKey)
        {
            Body = body;
            CacheKey = cacheKey;
        }

        public Dictionary<string, string> Headers { get; } = new();

        public byte[] Body 
        {
            get => _body;
            set
            {
                _body = value;
                _contentHash = null;
            }
        }

        public string CacheKey { get; set; }

        public IEnumerable<string> IncludedFiles { get; set; }

        [JsonIgnore]
        public string ContentHash
        {
            get => _contentHash ??= (_body != null && _body.Length > 0 ? ComputeHash(_body) : string.Empty);
            set => _contentHash = value;
        }

        internal static string ComputeHash(byte[] content)
        {
            using (var algo = SHA1.Create())
            {
                byte[] hash = algo.ComputeHash(content);
                return WebEncoders.Base64UrlEncode(hash);
            }
        }
    }
}
