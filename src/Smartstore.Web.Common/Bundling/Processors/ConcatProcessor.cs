using Smartstore.Utilities;

namespace Smartstore.Web.Bundling.Processors
{
    public class ConcatProcessor : BundleProcessor
    {
        internal static readonly ConcatProcessor Instance = new();

        public override string Code => "concat";

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

                context.ProcessorCodes.Add(Code);
            }

            return Task.CompletedTask;
        }
    }
}
