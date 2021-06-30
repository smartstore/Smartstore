using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Web.Bundling.Processors
{
    /// <summary>
    /// Rewrites urls to be absolute so assets will still be found after bundling
    /// </summary>
    public class CssRewriteUrlProcessor : BundleProcessor
    {
        internal static readonly CssRewriteUrlProcessor Instance = new();

        private static readonly Regex _rgUrl = new(@"url\s*\(\s*([""']?)([^:)]+)\1\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly string _protocol = "file:///";

        public override Task ProcessAsync(BundleContext context)
        {
            var env = context.HttpContext.RequestServices.GetService<IWebHostEnvironment>();

            foreach (var asset in context.Content)
            {
                var inputPath = Path.Combine(env.WebRootPath, asset.Path.TrimStart('/'));
                var outputPath = Path.Combine(env.WebRootPath, context.Bundle.Route.TrimStart('/'));
                asset.Content = Adjust(asset.Content, inputPath, outputPath);
            }

            return Task.CompletedTask;
        }

        private static string Adjust(string input, string inputFile, string outputPath)
        {
            // apply the RegEx to the file (to change relative paths)
            var matches = _rgUrl.Matches(input);

            // Ignore the file if no match
            if (matches.Count > 0)
            {
                var cssDirectoryPath = Path.GetDirectoryName(inputFile);

                foreach (Match match in matches)
                {
                    var quoteDelimiter = match.Groups[1].Value; //url('') vs url("")
                    var urlValue = match.Groups[2].Value;

                    // Ignore root relative references
                    if (urlValue.StartsWith('/') || urlValue.StartsWith("data:"))
                        continue;

                    // Prevent query string from causing error
                    var pathAndQuery = urlValue.Split(new[] { '?' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    var pathOnly = pathAndQuery[0];
                    var queryOnly = pathAndQuery.Length == 2 ? pathAndQuery[1] : string.Empty;

                    var absolutePath = GetAbsolutePath(cssDirectoryPath, pathOnly);
                    var serverRelativeUrl = MakeRelative(outputPath, absolutePath);

                    if (!string.IsNullOrEmpty(queryOnly))
                    {
                        serverRelativeUrl += "?" + queryOnly;
                    }

                    var replace = string.Format("url({0}{1}{0})", quoteDelimiter, serverRelativeUrl);

                    input = input.Replace(match.Groups[0].Value, replace);
                }
            }

            return input;
        }

        private static string GetAbsolutePath(string cssFilePath, string pathOnly)
        {
            return Path.GetFullPath(Path.Combine(cssFilePath, pathOnly));
        }

        private static string MakeRelative(string baseFile, string file)
        {
            if (string.IsNullOrEmpty(file))
                return file;

            // The file:// protocol is to make it work on Linux.
            // See https://github.com/madskristensen/BundlerMinifier/commit/01fe7a050eda073f8949caa90eedc4c23e04d0ce
            var baseUri = new Uri(_protocol + baseFile, UriKind.RelativeOrAbsolute);
            var fileUri = new Uri(_protocol + file, UriKind.RelativeOrAbsolute);

            if (baseUri.IsAbsoluteUri)
            {
                return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString());
            }
            else
            {
                return baseUri.ToString();
            }
        }
    }
}
