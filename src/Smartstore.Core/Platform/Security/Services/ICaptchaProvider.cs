#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Represents a CAPTCHA provider.
    /// </summary>
    public interface ICaptchaProvider : IProvider
    {
        /// <summary>
        /// Gets a value indicating whether the current CAPTCHA instance is properly configured.
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Gets a value indicating whether the CAPTCHA widget is invisible.
        /// </summary>
        bool IsInvisible { get; }

        /// <summary>
        /// Gets a value indicating whether the CAPTCHA is non-interactive (e.g.: reCAPTCHA v2 invisible OR reCAPTCHA v3).
        /// </summary>
        bool IsNonInteractive { get; }

        /// <summary>
        /// Creates the widget instance that displays the CAPTCHA challenge.
        /// </summary>
        Task<Widget?> CreateWidgetAsync(CaptchaContext context);

        /// <summary>
        /// Validates the CAPTCHA challenge and returns the validation result.
        /// </summary>
        Task<CaptchaValidationResult> ValidateAsync(CaptchaContext context, CancellationToken cancelToken = default);
    }

    #region Context and Result Classes

    /// <summary>
    /// Represents the context for a CAPTCHA operation, including the associated HTTP context and optional language
    /// settings.
    /// </summary>
    /// <remarks>This class encapsulates the HTTP context and language preferences for a CAPTCHA operation. 
    /// It is typically used to provide contextual information required for generating or validating CAPTCHA
    /// challenges.</remarks>
    public sealed class CaptchaContext(HttpContext httpContext, Language? language = null)
    {
        public HttpContext HttpContext { get; set; } = httpContext;

        public IPageAssetBuilder AssetBuilder
            => HttpContext.RequestServices.GetRequiredService<IPageAssetBuilder>();

        public IUrlHelper Url
            => HttpContext.RequestServices.GetRequiredService<IUrlHelper>();

        public Language? Language { get; set; } = language;
    }

    /// <summary>
    /// Represents the result of a CAPTCHA validation attempt.
    /// </summary>
    /// <remarks>This class encapsulates the outcome of a CAPTCHA validation, including whether the validation
    /// was successful and any associated messages providing additional context or details.</remarks>
    public sealed class CaptchaValidationResult
    {
        public bool Success { get; set; }

        public IList<CaptchaValidationMessage> Messages { get; } = [];
    }

    /// <summary>
    /// Represents a validation message generated during CAPTCHA processing.
    /// </summary>
    /// <remarks>This class encapsulates a message code and its associated severity level,  providing
    /// information about the outcome of CAPTCHA validation. Instances of this class are immutable.</remarks>
    public sealed class CaptchaValidationMessage(string code, CaptchaValidationMessageLevel level)
    {
        public CaptchaValidationMessage(string code, string? detail, CaptchaValidationMessageLevel level)
            : this(code, level)
        {
            Detail = detail;
        }

        public string Code { get; } = code;

        public string? Detail { get; set; }

        public CaptchaValidationMessageLevel Level { get; } = level;
    }

    /// <summary>
    /// Specifies the severity level of a CAPTCHA validation message.
    /// </summary>
    /// <remarks>This enumeration is used to indicate whether a CAPTCHA validation message represents a warning or an error.</remarks>
    public enum CaptchaValidationMessageLevel
    {
        Warning,
        Error
    }

    #endregion
}
