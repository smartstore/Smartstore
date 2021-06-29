using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Web.Bundling
{
    public class AssetContent
    {
        public string Path { get; init; }
        public DateTimeOffset LastModifiedUtc { get; init; }
        public string ContentType { get; init; }
        public string Content { get; set; }
        public bool IsMinified 
        { 
            get
            {
                // TODO: Implement AssetContent.IsMinified
                return false;
            } 
        } 
    }
    
    public class BundleContext
    {
        public HttpContext HttpContext { get; init; }
        public BundlingOptions Options { get; init; }
        public Bundle Bundle { get; init; }
        public IEnumerable<BundleFile> Files { get; init; }
        public IList<AssetContent> Content { get; } = new List<AssetContent>();
        public IList<string> ProcessorCodes { get; } = new List<string>();
        public IList<string> IncludedFiles { get; } = new List<string>();
    }
}
