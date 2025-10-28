using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Checks whether captcha is valid and - if not - outputs a notification.
    /// </summary>
    public sealed class ValidateCaptchaAttribute : TypeFilterAttribute
    {
        public ValidateCaptchaAttribute(string targetName)
            : base(typeof(ValidateCaptchaFilter))
        {
            Arguments = [this];
            CaptchaTargetName = targetName;
        }

        /// <summary>
        /// Gets or sets the name of the CAPTCHA target that indicates 
        /// whether the CAPTCHA is displayed ("PasswordRecovery" for example).
        /// Avoids unnecessary validation requests and "invalid-input-response" error if the CAPTCHA is not active at all.
        /// <see cref="CaptchaSettings.Targets.All"/> contains all valid targets.
        /// </summary>
        public string CaptchaTargetName { get; set; }
    }

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
            }
            catch (Exception ex)
            {
                Logger.ErrorsAll(ex);
            }

            // This will push the result value into a parameter in our action method.
            context.ActionArguments["captchaValid"] = valid;

            context.ActionArguments["captchaError"] = !valid && captchaProvider?.Value.IsConfigured == true
                ? T(captchaProvider?.Value.IsInvisible == true ? "Common.WrongInvisibleCaptcha" : "Common.WrongCaptcha").Value
                : null;

            await next();
        }

        private bool IsCaptchaActive()
            => _attribute.CaptchaTargetName.IsEmpty() || _captchaManager.IsActiveTarget(_attribute.CaptchaTargetName);
    }
}
