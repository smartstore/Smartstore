using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Utilities;

namespace Smartstore.Web.Bundling.Processors
{
    public class ConcatProcessor : BundleProcessor
    {
        public override Task ProcessAsync(BundleContext context)
        {
            if (context.Content.Count > 1)
            {
                using var psb = StringBuilderPool.Instance.Get(out var sb);

                foreach (var asset in context.Content)
                {
                    sb.Append(asset.Content);
                    sb.Append(context.Bundle.ConcatenationToken);
                }

                var combinedAsset = new AssetContent
                {
                    Content = sb.ToString(),
                    LastModifiedUtc = DateTimeOffset.UtcNow,
                    ContentType = context.Bundle.ContentType,
                    Path = Guid.NewGuid().ToString()
                };

                context.Content.Clear();
                context.Content.Add(combinedAsset);
            }

            return Task.CompletedTask;
        }
    }
}
