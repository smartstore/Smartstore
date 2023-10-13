using System.Globalization;
using DotLiquid;
using DotLiquid.FileSystems;
using DotLiquid.NamingConventions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;
using Smartstore.Core.Theming;
using Smartstore.Events;
using Smartstore.IO;
using IFileSystem = Smartstore.IO.IFileSystem;

namespace Smartstore.Templating.Liquid
{
    public partial class LiquidTemplateEngine : ITemplateEngine, ITemplateFileSystem
    {
        private readonly IApplicationContext _appContext;
        private readonly IMemoryCache _memCache;
        private readonly Work<LocalizerEx> _localizer;
        private readonly Work<IThemeContext> _themeContext;

        public LiquidTemplateEngine(
            IApplicationContext appContext,
            IEventPublisher eventPublisher,
            IMemoryCache memCache,
            Work<LocalizerEx> localizer,
            Work<IThemeContext> themeContext)
        {
            _appContext = appContext;
            _memCache = memCache;
            _localizer = localizer;
            _themeContext = themeContext;

            // Register Value type transformers
            var allowedMoneyMembers = new[]
            {
                nameof(Money.Amount),
                nameof(Money.RoundedAmount),
                nameof(Money.TruncatedAmount),
                nameof(Money.ToString),
                nameof(Money.DecimalDigits)
            };
            Template.RegisterSafeType(typeof(Money), allowedMoneyMembers, x => x);

            // Register tag "zone"
            Template.RegisterTagFactory(new ZoneTagFactory(eventPublisher));

            // Register Filters
            Template.RegisterFilter(typeof(AdditionalFilters));

            Template.NamingConvention = new CSharpNamingConvention();
            Template.FileSystem = this;
        }

        public LocalizedString T(string key, int languageId, params object[] args)
        {
            return _localizer.Value(key, languageId, args);
        }

        #region ITemplateEngine

        public ITemplate Compile(string source)
        {
            Guard.NotNull(source);

            return new LiquidTemplate(Template.Parse(source), source);
        }

        public Task<string> RenderAsync(string source, object model, IFormatProvider formatProvider = null)
        {
            Guard.NotNull(source);
            Guard.NotNull(model);

            return Compile(source).RenderAsync(model, formatProvider ?? CultureInfo.CurrentCulture);
        }

        public ITestModel CreateTestModelFor(BaseEntity entity, string modelPrefix)
        {
            Guard.NotNull(entity);

            return new TestDrop(entity, modelPrefix);
        }

        #endregion

        #region ITemplateFileSystem

        public Template GetTemplate(Context context, string templateName)
        {
            var path = NormalizeTemplatePath(context, templateName, out var fs, out var theme);
            if (path.IsEmpty() || fs == null)
            {
                return null;
            }

            var cacheKey = _memCache.BuildScopedKey("Liquid://" + theme.RightPad(pad: '/') + path);
            var cachedTemplate = _memCache.Get(cacheKey);

            if (cachedTemplate == null)
            {
                // Read from file, compile and put to cache with file dependeny
                var file = fs.GetFile(path);
                var source = ReadTemplateFileInternal(file);

                cachedTemplate = Template.Parse(source);
                using var entry = _memCache.CreateEntry(cacheKey);
                entry.Value = cachedTemplate;
                entry.ExpirationTokens.Add(fs.Watch(path) ?? NullChangeToken.Singleton);
            }

            return (Template)cachedTemplate;
        }

        public string ReadTemplateFile(Context context, string templateName)
        {
            var path = NormalizeTemplatePath(context, templateName, out var fs, out _);
            return ReadTemplateFileInternal(fs?.GetFile(path));
        }

        private static string ReadTemplateFileInternal(IFile file)
        {
            if (file == null)
            {
                return string.Empty;
            }

            if (!file.Exists)
            {
                throw new FileNotFoundException($"Template file '{file.SubPath}' does not exist.");
            }

            return file.ReadAllText();
        }

        private string NormalizeTemplatePath(Context context, string path, out IFileSystem fs, out string theme)
        {
            theme = null;
            path = ((string)context[path]).NullEmpty() ?? path;

            if (path.IsEmpty())
            {
                fs = null;
                return null;
            }

            if (!path.StartsWith("~/"))
            {
                path = PathUtility.Join(
                    "Views/Shared/EmailTemplates",
                    PathUtility.NormalizeRelativePath(path).EnsureEndsWith(".liquid"));

                fs = _themeContext.Value.CurrentTheme?.ContentRoot;
                theme = _themeContext.Value.CurrentTheme?.Name;
                return path;
            }
            else
            {
                fs = _appContext.ContentRoot;
                return path;
            }
        }

        #endregion
    }
}
