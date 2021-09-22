using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Pdf.WkHtml
{
    public interface IWkHtmlCommandBuilder
    {
        Task<IReadOnlyList<PdfInput>> BuildCommandAsync(PdfConversionSettings settings, StringBuilder builder);
    }

    // TODO: (core) Initialize StringBuilder with CustomArgs
    // TODO: (core) Implement PdfHtmlContent & PdfUrlContent
    // TODO: (core) CustomArgs should skip build process (?)
    // TODO: (core) ToolPath
    // TODO: (core) TempFilesPath
    // TODO: (core) Apply cover & toc (?) Converter Line 118
    public partial class WkHtmlCommandBuilder : IWkHtmlCommandBuilder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public WkHtmlCommandBuilder(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public virtual async Task<IReadOnlyList<PdfInput>> BuildCommandAsync(PdfConversionSettings settings, StringBuilder builder)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var inputs = new List<PdfInput>(4);

            // Global
            BuildGlobalCommandFragment(settings, builder);

            // Main page
            inputs.Add(settings.Page);
            await settings.Page.ProcessInputAsync("page", httpContext);
            settings.Page.BuildCommandFragment("page", httpContext, builder);
            if (settings.PageOptions != null)
            {
                BuildPageCommandFragment(settings.PageOptions, builder);
            }

            // Cover
            if (settings.Cover != null)
            {
                inputs.Add(settings.Cover);
                var path = await settings.Cover.ProcessInputAsync("cover", httpContext);
                if (path.HasValue())
                {
                    TryAppendOption("cover", path, builder);
                    settings.Cover.BuildCommandFragment("cover", httpContext, builder);
                    if (settings.CoverOptions != null)
                    {
                        BuildPageCommandFragment(settings.CoverOptions, builder);
                    }
                }
            }

            // TOC options
            if (settings.TocOptions != null && settings.TocOptions.Enabled)
            {
                BuildTocCommandFragment(settings.TocOptions, builder);
            }

            // Header content & options
            if (settings.Header != null)
            {
                inputs.Add(settings.Header);
                var path = await settings.Header.ProcessInputAsync("header", httpContext);
                if (path.HasValue())
                {
                    TryAppendOption("--header-html", path, builder);
                    settings.Header.BuildCommandFragment("header", httpContext, builder);
                }
            }
            if (settings.HeaderOptions != null && (settings.Header != null || settings.HeaderOptions.HasText))
            {
                BuildSectionCommandFragment(settings.HeaderOptions, "header", builder);
            }

            // Footer content & options
            if (settings.Footer != null)
            {
                inputs.Add(settings.Footer);
                var path = await settings.Footer.ProcessInputAsync("footer", httpContext);
                if (path.HasValue())
                {
                    TryAppendOption("--footer-html", path, builder);
                    settings.Footer.BuildCommandFragment("footer", httpContext, builder);
                }
            }
            if (settings.FooterOptions != null && (settings.Footer != null || settings.FooterOptions.HasText))
            {
                BuildSectionCommandFragment(settings.FooterOptions, "footer", builder);
            }

            return inputs;
        }

        protected virtual void BuildGlobalCommandFragment(PdfConversionSettings settings, StringBuilder builder)
        {
            // Commons
            TryAppendOption("--quiet", settings.Quiet, builder);
            TryAppendOption("-g", settings.Grayscale, builder);
            TryAppendOption("-l", settings.LowQuality, builder);
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
        }

        protected virtual void BuildPageCommandFragment(PdfPageOptions options, StringBuilder builder)
        {
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
            builder.Append(" toc");

            TryAppendOption("--toc-header-text", options.TocHeaderText, builder);
            TryAppendOption("--toc-level-indentation", options.TocLevelIndendation, builder, false);
            TryAppendOption("--toc-text-size-shrink", options.TocTextSizeShrink, builder);
            TryAppendOption("--disable-dotted-lines", options.DisableDottedLines, builder);
            TryAppendOption("--disable-toc-links", options.DisableTocLinks, builder);

            BuildPageCommandFragment(options, builder);
        }

        protected virtual void BuildSectionCommandFragment(PdfSectionOptions options, string flag, StringBuilder builder)
        {
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
