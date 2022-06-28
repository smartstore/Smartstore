using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Http;
using Smartstore.Net;

namespace Smartstore.Pdf.WkHtml
{
    public interface IWkHtmlCommandBuilder
    {
        Task BuildCommandAsync(PdfConversionSettings settings, StringBuilder builder);
    }

    public partial class WkHtmlCommandBuilder : IWkHtmlCommandBuilder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WkHtmlCommandBuilder(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public virtual async Task BuildCommandAsync(PdfConversionSettings settings, StringBuilder builder)
        {
            // Global
            BuildGlobalCommandFragment(settings, builder);

            // Cover
            if (settings.Cover != null)
            {
                await ProcessInputAsync("cover", settings.Cover);
                if (settings.Cover.Content.HasValue())
                {
                    TryAppendOption("cover", settings.Cover.Content, builder);
                    BuildPageCommandFragment(settings.CoverOptions, builder);
                }
            }

            // TOC options
            if (settings.TocOptions != null && settings.TocOptions.Enabled)
            {
                BuildTocCommandFragment(settings.TocOptions, builder);
            }

            // Main page input
            await ProcessInputAsync("page", settings.Page);
            var content = settings.Page.Kind == PdfInputKind.Html
                ? "-" // we gonna pump in via StdInput
                : settings.Page.Content;
            builder.Append($" \"{content}\"");

            // Main page options
            BuildPageCommandFragment(settings.PageOptions, builder);

            // Header content & options
            await ProcessSectionAsync("header", settings.Header, settings.HeaderOptions, builder);

            // Footer content & options
            await ProcessSectionAsync("footer", settings.Footer, settings.FooterOptions, builder);

            // INFO: Output file comes later in converter
        }

        #region Input processing

        private async Task ProcessSectionAsync(string flag, IPdfInput input, PdfSectionOptions options, StringBuilder builder)
        {
            if (input != null)
            {
                await ProcessInputAsync(flag, input);
                var content = input.Content;
                if (content.HasValue())
                {
                    TryAppendOption($"--{flag}-html", content, builder);
                    BuildSectionCommandFragment(input, options, flag, builder);
                }
            }
        }

        protected virtual Task ProcessInputAsync(string flag, IPdfInput input)
        {
            if (input is WkHtmlInput html)
            {
                return html.ProcessAsync(flag);
            }
            else if (input is WkFileInput)
            {
                // No processable content
                return Task.CompletedTask;
            }

            throw new ArgumentException($"Unknown input type '{input?.GetType()?.Name.NaIfEmpty()}'.", nameof(input));
        }

        #endregion

        #region Options

        protected virtual void BuildGlobalCommandFragment(PdfConversionSettings settings, StringBuilder builder)
        {
            // Commons
            TryAppendOption("--quiet", true, builder);
            TryAppendOption("--encoding", "utf-8", builder, false);
            TryAppendOption("-g", settings.Grayscale, builder);
            TryAppendOption("-l", settings.LowQuality, builder);
            TryAppendOption("--outline-depth", settings.OutlineDepth, builder);
            TryAppendOption("--title", settings.Title, builder);

            // Margins
            TryAppendOption("-B", settings.Margins?.Bottom, builder);
            TryAppendOption("-L", settings.Margins?.Left, builder);
            TryAppendOption("-R", settings.Margins?.Right, builder);
            TryAppendOption("-T", settings.Margins?.Top, builder);

            // Size & Orientation
            TryAppendOption("-O", settings.Orientation?.ToString(), builder, false);
            TryAppendOption("-s", settings.Size?.ToString(), builder, false);
            TryAppendOption("--page-width", settings.PageWidth, builder);
            TryAppendOption("--page-height", settings.PageHeight, builder);

            // Custom global args
            if (settings.CustomArguments.HasValue())
            {
                builder.Append(" " + settings.CustomArguments);
            }
        }

        protected virtual void BuildPageCommandFragment(PdfPageOptions options, StringBuilder builder)
        {
            if (options == null)
                return;

            TryAppendOption("--user-style-sheet", options.UserStylesheetUrl, builder);
            TryAppendOption("--print-media-type", options.UsePrintMediaType, builder);
            TryAppendOption("--no-background", options.DisableBackground, builder);
            TryAppendOption("--no-images", options.DisableImages, builder);
            TryAppendOption("--enable-plugins", options.EnablePlugins, builder);
            TryAppendOption("--disable-javascript", options.DisableJavascript, builder);
            TryAppendOption("--disable-smart-shrinking", options.DisableSmartShrinking, builder);
            TryAppendOption("--minimum-font-size", options.MinimumFontSize, builder);

            if (options.Zoom != 1)
            {
                builder.Append(" --zoom " + options.Zoom.ToStringInvariant());
            }

            if (options.CustomArguments.HasValue())
            {
                builder.Append(" " + options.CustomArguments);
            }
        }

        protected virtual void BuildTocCommandFragment(PdfTocOptions options, StringBuilder builder)
        {
            if (options == null)
                return;

            builder.Append(" toc");

            TryAppendOption("--toc-header-text", options.TocHeaderText.Replace("\"", "\\\""), builder);
            TryAppendOption("--toc-level-indentation", options.TocLevelIndendation, builder, false);
            TryAppendOption("--toc-text-size-shrink", options.TocTextSizeShrink, builder);
            TryAppendOption("--disable-dotted-lines", options.DisableDottedLines, builder);
            TryAppendOption("--disable-toc-links", options.DisableTocLinks, builder);

            BuildPageCommandFragment(options, builder);
        }

        protected virtual void BuildSectionCommandFragment(IPdfInput input, PdfSectionOptions options, string flag, StringBuilder builder)
        {
            if (options == null)
                return;

            // Send auth cookie if input is a file and points to a url local to host.
            if (input is WkFileInput fileInput && fileInput.IsLocalUrl)
            {
                var httpRequest = _httpContextAccessor.HttpContext?.Request;
                if (httpRequest != null)
                {
                    if (httpRequest.Cookies.TryGetValue(CookieNames.Identity, out var cookieValue))
                    {
                        TryAppendOption("--cookie", $"{CookieNames.Identity} {cookieValue.UrlEncode()}", builder, false);
                    }
                    else if (httpRequest.Cookies.TryGetValue(CookieNames.Visitor, out cookieValue))
                    {
                        TryAppendOption("--cookie", $"{CookieNames.Visitor} {cookieValue.UrlEncode()}", builder, false);
                    }
                }
            }

            TryAppendOption(() => $"--{flag}-spacing", options.Spacing, builder);
            TryAppendOption(() => $"--{flag}-line", options.ShowLine, builder);

            if (options.HasText)
            {
                TryAppendOption(() => $"--{flag}-font-name", options.FontName, builder);
                TryAppendOption(() => $"--{flag}-font-size", options.FontSize, builder);
            }

            TryAppendOption(() => $"--{flag}-left", options.TextLeft, builder);
            TryAppendOption(() => $"--{flag}-center", options.TextCenter, builder);
            TryAppendOption(() => $"--{flag}-right", options.TextRight, builder);

            if (options.CustomArguments.HasValue())
            {
                builder.Append(" " + options.CustomArguments);
            }
        }

        #endregion

        #region TryAppendOption...

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void TryAppendOption(Func<string> option, string value, StringBuilder builder, bool quote = true)
        {
            if (value.HasValue())
            {
                builder.Append(' ' + option.Invoke());
                if (quote)
                {
                    builder.Append(" \"" + value.Replace("\"", "\\\"") + "\"");
                }
                else
                {
                    builder.Append(' ' + value);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void TryAppendOption(string option, string value, StringBuilder builder, bool quote = true)
        {
            if (value.HasValue())
            {
                builder.Append(' ' + option);
                if (quote)
                {
                    builder.Append(" \"" + value.Replace("\"", "\\\"") + "\"");
                }
                else
                {
                    builder.Append(' ' + value);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void TryAppendOption(Func<string> option, float? value, StringBuilder builder)
        {
            if (value != null)
            {
                builder.Append(' ' + option.Invoke() + ' ' + value.Value.ToStringInvariant());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void TryAppendOption(string option, float? value, StringBuilder builder)
        {
            if (value != null)
            {
                builder.Append(' ' + option + ' ' + value.Value.ToStringInvariant());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void TryAppendOption(string option, int? value, StringBuilder builder)
        {
            if (value != null)
            {
                builder.Append(' ' + option + ' ' + value.Value.ToStringInvariant());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void TryAppendOption(Func<string> option, bool value, StringBuilder builder)
        {
            if (value)
            {
                builder.Append(' ' + option.Invoke());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void TryAppendOption(string option, bool value, StringBuilder builder)
        {
            if (value)
            {
                builder.Append(' ' + option);
            }
        }

        #endregion
    }
}
