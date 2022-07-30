using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.FileProviders;
using Smartstore.Core.Theming;
using Smartstore.Web.Bundling.Processors;

namespace Smartstore.Web.Bundling
{
    public class DynamicBundleContext
    {
        public PathString Path { get; set; }
        public RouteValueDictionary RouteValues { get; init; }
        public DynamicBundle Bundle { get; set; }
        public HttpContext HttpContext { get; init; }
        public IApplicationContext ApplicationContext { get; init; }
        public IThemeRegistry ThemeRegistry { get; init; }
        public BundlingOptions BundlingOptions { get; init; }
    }

    [DebuggerDisplay("DynamicBundle: {Route}")]
    internal class DynamicBundleMatch : Bundle
    {
        private string[] _sourceFiles;

        public DynamicBundleMatch(DynamicBundleContext context)
            : base(context.Path, context.Bundle.ContentType, context.Bundle.FileProvider, context.Bundle.Processors.ToArray())
        {
            DynamicBundleContext = Guard.NotNull(context, nameof(context));
        }

        public DynamicBundleContext DynamicBundleContext { get; }

        public override IEnumerable<string> SourceFiles
        {
            get => _sourceFiles ??= DynamicBundleContext.Bundle.ResolveSourceFiles(DynamicBundleContext).ToArray();
        }
    }

    /// <summary>
    /// Represents a dynamic script bundle that does Js minification.
    /// </summary>
    public class DynamicScriptBundle : DynamicBundle
    {
        public DynamicScriptBundle(string routeTemplate, object defaults = null)
            : base(routeTemplate, defaults, "application/javascript", null, JsMinifyProcessor.Instance, ConcatProcessor.Instance)
        {
            ConcatenationToken = ";" + Environment.NewLine;
        }
    }

    /// <summary>
    /// Represents a dynamic stylesheet bundle that does CSS minification, URL rewrite & Autoprefixing.
    /// </summary>
    public class DynamicStyleBundle : DynamicBundle
    {
        public DynamicStyleBundle(string routeTemplate, object defaults = null)
            : base(routeTemplate, defaults, "text/css", null, SassProcessor.Instance, CssMinifyProcessor.Instance, CssRewriteUrlProcessor.Instance, ConcatProcessor.Instance, AutoprefixerProcessor.Instance)
        {
        }
    }

    /// <summary>
    /// Represents a dynamic late-bound list of file references to be bundled together as a single resource.
    /// </summary>
    public class DynamicBundle : Bundle
    {
        private static readonly ConcurrentDictionary<string, string[]> _sourceFilesCache = new();

        private readonly List<Func<DynamicBundleContext, bool>> _constraints = new();
        private readonly List<Func<DynamicBundleContext, IEnumerable<string>>> _resolvers = new();

        public DynamicBundle(
            string routeTemplate,
            object defaults,
            string contentType,
            IFileProvider fileProvider,
            params IBundleProcessor[] processors)
            : base(routeTemplate, contentType, fileProvider, processors)
        {
            routeTemplate = base.Route;

            Defaults = new RouteValueDictionary(defaults);
            TemplateMatcher = new TemplateMatcher(TemplateParser.Parse(routeTemplate), Defaults);
        }

        public RouteValueDictionary Defaults { get; }
        public TemplateMatcher TemplateMatcher { get; }

        protected override string ValidateRoute(string route)
        {
            return route;
        }

        public override Bundle Include(params string[] paths)
        {
            throw new NotSupportedException("Adding static files to dynamic bundles is not supported. Call the 'Include(Func<...> fileResolver)' method which takes a resolver delegate instead.");
        }

        public DynamicBundle Include(Func<DynamicBundleContext, IEnumerable<string>> fileResolver)
        {
            _resolvers.Add(Guard.NotNull(fileResolver, nameof(fileResolver)));
            return this;
        }

        public DynamicBundle WithConstraint(Func<DynamicBundleContext, bool> constraint)
        {
            _constraints.Add(Guard.NotNull(constraint, nameof(constraint)));
            return this;
        }

        internal bool TryMatchRoute(PathString path, out RouteValueDictionary values)
        {
            values = new RouteValueDictionary();
            if (TemplateMatcher.TryMatch(path, values))
            {
                return true;
            }

            values = null;
            return false;
        }

        internal bool IsStatisfiedByConstraints(DynamicBundleContext context)
        {
            if (_constraints.Count == 0)
            {
                return true;
            }

            return _constraints.All(c => c(context));
        }

        internal virtual IEnumerable<string> ResolveSourceFiles(DynamicBundleContext context)
        {
            if (context.RouteValues.Count == 0)
            {
                return _resolvers.SelectMany(resolver => resolver(context));
            }

            var cacheKey = BuildSourceFilesCacheKey(context.RouteValues);
            return _sourceFilesCache.GetOrAdd(cacheKey, key
                => _resolvers.SelectMany(resolver => resolver(context)).ToArray());
        }

        private static string BuildSourceFilesCacheKey(RouteValueDictionary values)
        {
            var key = string.Empty;
            foreach (var kvp in values)
            {
                key += kvp.Key + ':' + kvp.Value.ToString() + '_';
            }

            return key;
        }
    }
}
