using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Smartstore.Core.Localization;
using Smartstore.Engine;

namespace Smartstore.Core.Security
{
    public class GoogleRecaptchaApiResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("error-codes")]
        public List<string> ErrorCodes { get; set; }
    }

    /// <summary>
    /// Checks whether captcha is valid and - if not - outputs a notification.
    /// </summary>
    public class ValidateCaptchaAttribute : TypeFilterAttribute
    {
        public ValidateCaptchaAttribute() 
            : base(typeof(ValidateCaptchaFilter))
        {
        }

        class ValidateCaptchaFilter : IAsyncActionFilter
        {
            private readonly CaptchaSettings _captchaSettings;
            private readonly SmartConfiguration _appConfig;

            public ValidateCaptchaFilter(
                CaptchaSettings captchaSettings, 
                ILogger<ValidateCaptchaFilter> logger, 
                Localizer localizer,
                SmartConfiguration appConfig)
            {
                _captchaSettings = captchaSettings;
                _appConfig = appConfig;
                Logger = logger;
                T = localizer;
            }

            public ILogger Logger { get; set; } = NullLogger.Instance;
            public Localizer T { get; set; } = NullLocalizer.Instance;

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var valid = false;

                try
                {
                    if (_captchaSettings.CanDisplayCaptcha && context.HttpContext.Request.HasFormContentType)
                    {
                        var verifyUrl = _appConfig.Google.RecaptchaVerifyUrl;
                        var recaptchaResponse = context.HttpContext.Request.Form["g-recaptcha-response"];

                        var url = "{0}?secret={1}&response={2}".FormatInvariant(
                            verifyUrl,
                            _captchaSettings.ReCaptchaPrivateKey.UrlEncode(),
                            recaptchaResponse.ToString().UrlEncode()
                        );

                        using var webClient = new WebClient();
                        var jsonResponse = await webClient.DownloadStringTaskAsync(url);
                        var result = JsonConvert.DeserializeObject<GoogleRecaptchaApiResponse>(jsonResponse);

                        if (result == null)
                        {
                            Logger.Error(T("Common.CaptchaUnableToVerify"));
                        }
                        else
                        {
                            if (result.ErrorCodes == null)
                            {
                                valid = result.Success;
                            }
                            else
                            {
                                // Do not log 'missing input'. Could be a regular case.
                                foreach (var error in result.ErrorCodes.Where(x => x.HasValue() && x != "missing-input-response"))
                                {
                                    Logger.Error("Error while getting Google Recaptcha response: " + error);
                                }
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

                context.ActionArguments["captchaError"] = !valid && _captchaSettings.CanDisplayCaptcha
                    ? T(_captchaSettings.UseInvisibleReCaptcha ? "Common.WrongInvisibleCaptcha" : "Common.WrongCaptcha").Value
                    : null;

                await next();
            }
        }
    }
}
