using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
//using Newtonsoft.Json;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CaptchaSettings _captchaSettings;
        private readonly IProviderManager _providerManager;
        //private readonly SmartConfiguration _appConfig;

        public ValidateCaptchaFilter(
            ValidateCaptchaAttribute attribute,
            IHttpClientFactory httpClientFactory,
            CaptchaSettings captchaSettings,
            ILogger<ValidateCaptchaFilter> logger,
            Localizer localizer,
            //SmartConfiguration appConfig,
            IProviderManager providerManager)
        {
            _attribute = attribute;
            _httpClientFactory = httpClientFactory;
            _captchaSettings = captchaSettings;
            //_appConfig = appConfig;
            _providerManager = providerManager;

            Logger = logger;
            T = localizer;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var valid = false;

            ICaptchaProvider captchaProvider = null;

            try
            {
                if (_captchaSettings.Enabled && context.HttpContext.Request.HasFormContentType && IsCaptchaActive())
                {
                    captchaProvider = _providerManager.GetProvider<ICaptchaProvider>(_captchaSettings.ProviderSystemName)?.Value;
                    if (captchaProvider != null && captchaProvider.IsConfigured)
                    {
                        var captchaContext = new CaptchaContext(context.HttpContext);
                        var result = await captchaProvider.ValidateAsync(captchaContext);

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
            }
            catch (Exception ex)
            {
                Logger.ErrorsAll(ex);
            }

            // This will push the result value into a parameter in our action method.
            context.ActionArguments["captchaValid"] = valid;

            context.ActionArguments["captchaError"] = !valid && captchaProvider?.IsConfigured == true
                ? T(_captchaSettings.UseInvisibleReCaptcha ? "Common.WrongInvisibleCaptcha" : "Common.WrongCaptcha").Value
                : null;

            await next();
        }

        //public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        //{
        //    var valid = false;

        //    try
        //    {
        //        if (_captchaSettings.CanDisplayCaptcha && context.HttpContext.Request.HasFormContentType && IsCaptchaDisplayed())
        //        {
        //            var client = _httpClientFactory.CreateClient();
        //            var verifyUrl = _appConfig.Google.RecaptchaVerifyUrl;
        //            var recaptchaResponse = context.HttpContext.Request.Form["g-recaptcha-response"];

        //            var url = "{0}?secret={1}&response={2}".FormatInvariant(
        //                verifyUrl,
        //                _captchaSettings.ReCaptchaPrivateKey.UrlEncode(),
        //                recaptchaResponse.ToString().UrlEncode()
        //            );

        //            var jsonResponse = await client.GetStringAsync(url);
        //            var result = JsonConvert.DeserializeObject<GoogleRecaptchaApiResponse>(jsonResponse);

        //            if (result == null)
        //            {
        //                Logger.Error(T("Common.CaptchaUnableToVerify"));
        //            }
        //            else
        //            {
        //                if (result.ErrorCodes == null)
        //                {
        //                    valid = result.Success;
        //                }
        //                else
        //                {
        //                    // Do not log 'missing input'. Could be a regular case.
        //                    foreach (var error in result.ErrorCodes.Where(x => x.HasValue() && x != "missing-input-response"))
        //                    {
        //                        var msg = T("Common.ReCaptchaCheckFailed", error);
        //                        if (error == "invalid-input-response")
        //                        {
        //                            Logger.Warn(msg);
        //                        }
        //                        else
        //                        {
        //                            Logger.Error(msg);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.ErrorsAll(ex);
        //    }

        //    // This will push the result value into a parameter in our action method.
        //    context.ActionArguments["captchaValid"] = valid;

        //    context.ActionArguments["captchaError"] = !valid && _captchaSettings.CanDisplayCaptcha
        //        ? T(_captchaSettings.UseInvisibleReCaptcha ? "Common.WrongInvisibleCaptcha" : "Common.WrongCaptcha").Value
        //        : null;

        //    await next();
        //}

        private bool IsCaptchaActive()
        {
            if (_attribute.CaptchaTargetName.HasValue())
            {
                return _captchaSettings.IsActiveTarget(_attribute.CaptchaTargetName);
            }

            return true;
        }
    }
}
