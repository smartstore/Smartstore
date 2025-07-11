using NUglify;

namespace Smartstore.Web.Bundling.Processors
{
    public abstract class NUglifyProcessor : BundleProcessor
    {
        public override string Code => BundleProcessorCodes.Minify;

        public override Task ProcessAsync(BundleContext context)
        {
            if (context.Options.EnableMinification == false || context.ProcessorCodes.Contains(Code))
            {
                return Task.CompletedTask;
            }

            foreach (var asset in context.Content)
            {
                if (asset.IsMinified)
                {
                    continue;
                }

                try
                {
                    var result = MinifyCore(asset.Content);
                    var minCode = result.Code;

                    if (result.HasErrors)
                    {
                        HandleError(asset, string.Join(Environment.NewLine + Environment.NewLine, result.Errors.Select(x => x.ToString())));
                    }
                    else
                    {
                        context.ProcessorCodes.Add(Code);
                    }

                    asset.Content = minCode;
                }
                catch (Exception ex)
                {
                    HandleError(asset, ex.ToAllMessages());
                }
            }

            return Task.CompletedTask;
        }

        protected internal abstract UglifyResult MinifyCore(string source);

        protected virtual void HandleError(AssetContent asset, string message)
        {
            asset.Content = "/* NUGLIFY ERRORS: " + Environment.NewLine + message + " */" + Environment.NewLine + asset.Content;
        }
    }
}
