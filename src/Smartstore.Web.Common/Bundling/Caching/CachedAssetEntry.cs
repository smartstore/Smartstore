using System;
using System.Collections.Generic;

namespace Smartstore.Web.Bundling
{
    public class CachedAssetEntry
    {
        public string Content { get; set; }
        public string OriginalRoute { get; set; }
        public string PhysicalPath { get; set; }
        public IEnumerable<string> IncludedFiles { get; set; }
        public string HashCode { get; set; }
        public string ThemeName { get; set; }
        public int StoreId { get; set; }
        public string[] ProcessorCodes { get; set; }
    }
}
