using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Smartstore.Web.Bundling.Processors
{
    internal class CssImageInlinerProcessor : BundleProcessor
    {
        internal static readonly CssImageInlinerProcessor Instance = new();

        public override Task ProcessAsync(BundleContext context)
        {
            foreach (var asset in context.Content)
            {
                // TODO: (core) Implement...
            }

            return Task.CompletedTask;
        }
    }
}
