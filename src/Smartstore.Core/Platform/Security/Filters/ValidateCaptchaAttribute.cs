using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Attribute that enables provider-agnostic CAPTCHA validation via the ValidateCaptchaFilter.
    /// Only carries the logical "target name" to decide whether CAPTCHA is active for the current action.
    /// </summary>
    public sealed class ValidateCaptchaAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Creates a new attribute instance for the given logical target.
        /// Example targets could be "PasswordRecovery", "ContactUs", etc.
        /// </summary>
        /// <param name="targetName">
        /// Logical target name used by ICaptchaManager to determine whether CAPTCHA is active.
        /// Pass an empty string to always validate when a provider is configured.
        /// </param>
        public ValidateCaptchaAttribute(string targetName)
            : base(typeof(ValidateCaptchaFilter))
        {
            Arguments = [this];
            CaptchaTargetName = targetName;
        }

        /// <summary>
        /// Gets or sets the logical CAPTCHA target name that indicates whether CAPTCHA is active
        /// for the current action. This avoids unnecessary validation when CAPTCHA is disabled
        /// for the given target. Use empty string to always validate.
        /// <see cref="CaptchaSettings.Targets.All"/> contains all valid targets.
        /// </summary>
        public string CaptchaTargetName { get; set; }
    }

    /// <summary>
    /// MVC action filter that validates CAPTCHA in a provider-agnostic way.
    /// - Only runs when: request has a form, target is active, and a provider is configured.
    /// - Pushes two values into ActionArguments:
    ///     "captchaValid" : bool  -> indicates whether validation succeeded
    ///     "captchaError" : string? -> localized message suitable for UI (null when valid or not configured)
    /// - Logs warnings/errors based on validation messages returned by the provider.
    /// </summary>
    internal class ValidateCaptchaFilter : IAsyncActionFilter
    {
        private readonly ValidateCaptchaAttribute _attribute;
        private readonly ICaptchaManager _captchaManager;

        public ValidateCaptchaFilter(
            ValidateCaptchaAttribute attribute,
            ILogger<ValidateCaptchaFilter> logger,
            Localizer localizer,
            ICaptchaManager captchaManager)
        {
            _attribute = attribute;
            _captchaManager = captchaManager;

            Logger = logger;
            T = localizer;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var valid = false;
            Provider<ICaptchaProvider> captchaProvider = null;

            try
            {
                if (
                    context.HttpContext.Request.HasFormContentType
                    && IsCaptchaActive() 
                    && _captchaManager.IsConfigured(out captchaProvider))
                {
                    var captchaContext = new CaptchaContext(context.HttpContext);
                    var result = await captchaProvider.Value.ValidateAsync(captchaContext);

                    if (result.Success)
                    {
                        valid = true;
                    }
                    else if (result.Messages.Count > 0)
                    {
                        foreach (var message in result.Messages)
                        {
                            var text = T("Common.CaptchaCheckFailed", message.Code).Value;
                            if (message.Level == CaptchaValidationMessageLevel.Warning)
                            {
                                Logger.Warn(text);
                            }
                            else
                            {
                                Logger.Error(text);
                            }
                        }
                    }
                    else
                    {
                        Logger.Error(T("Common.CaptchaUnableToVerify").Value);
                    }
                }
                else
                {
                    valid = true;
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorsAll(ex);
            }

            // Expose validation outcome to the action
            context.ActionArguments["captchaValid"] = valid;

            // Provide a user-facing error string only when a provider is configured but validation failed
            var nonInteractive = (captchaProvider?.Value?.IsNonInteractive == true);
            var captchaError = !valid && captchaProvider?.Value.IsConfigured == true
                ? T(nonInteractive ? "Common.WrongInvisibleCaptcha" : "Common.WrongCaptcha").Value
                : null;
            context.ActionArguments["captchaError"] = captchaError;

            if (!valid && captchaError.HasValue())
            {
                context.ModelState.AddModelError(string.Empty, captchaError);
            }

            await next();
        }

        private bool IsCaptchaActive()
            => _attribute.CaptchaTargetName.IsEmpty() || _captchaManager.IsActiveTarget(_attribute.CaptchaTargetName);
    }
}
