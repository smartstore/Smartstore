using System;
using System.IO;

namespace Smartstore.Core.Content.Seo
{
    public class XmlSitemapPartition
    {
        public string Name { get; init; }
        public int Index { get; init; }
        public int StoreId { get; init; }
        public int LanguageId { get; init; }
        public DateTime ModifiedOnUtc { get; init; }
        public Stream Stream { get; init; }
    }
}
