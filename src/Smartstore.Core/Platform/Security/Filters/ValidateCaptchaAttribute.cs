using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Smartstore.Core.Localization;

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
    public sealed class ValidateCaptchaAttribute : TypeFilterAttribute
    {
        public ValidateCaptchaAttribute()
            : base(typeof(ValidateCaptchaFilter))
        {
            Arguments = new object[] { this };
        }

        /// <summary>
        /// Gets or sets the name of the <see cref="CaptchaSettings"/> property that indicates 
        /// whether the captcha is displayed ("ShowOnContactUsPage" for example).
        /// Avoids unnecessary validation requests and "invalid-input-response" error if the captcha is not displayed at all.
        /// </summary>
        public string CaptchaSettingName { get; set; }
    }

    internal class ValidateCaptchaFilter : IAsyncActionFilter
    {
        private readonly ValidateCaptchaAttribute _attribute;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CaptchaSettings _captchaSettings;
        private readonly SmartConfiguration _appConfig;

        public ValidateCaptchaFilter(
            ValidateCaptchaAttribute attribute,
            IHttpClientFactory httpClientFactory,
            CaptchaSettings captchaSettings,
            ILogger<ValidateCaptchaFilter> logger,
            Localizer localizer,
            SmartConfiguration appConfig)
        {
            _attribute = attribute;
            _httpClientFactory = httpClientFactory;
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
                if (_captchaSettings.CanDisplayCaptcha && context.HttpContext.Request.HasFormContentType && IsCaptchaDisplayed())
                {
                    var client = _httpClientFactory.CreateClient();
                    var verifyUrl = _appConfig.Google.RecaptchaVerifyUrl;
                    var recaptchaResponse = context.HttpContext.Request.Form["g-recaptcha-response"];

                    var url = "{0}?secret={1}&response={2}".FormatInvariant(
                        verifyUrl,
                        _captchaSettings.ReCaptchaPrivateKey.UrlEncode(),
                        recaptchaResponse.ToString().UrlEncode()
                    );

                    var jsonResponse = await client.GetStringAsync(url);
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

        private bool IsCaptchaDisplayed()
        {
            if (_attribute.CaptchaSettingName.HasValue())
            {
                var pi = _captchaSettings.GetType().GetProperty(_attribute.CaptchaSettingName);
                if (pi != null)
                {
                    var propValue = pi.GetValue(_captchaSettings);
                    if (propValue is bool displayCaptcha)
                    {
                        return displayCaptcha;
                    }
                }
            }

            return true;
        }
    }
}
