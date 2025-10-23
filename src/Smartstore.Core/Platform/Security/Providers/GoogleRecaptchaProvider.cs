using System.Net.Http;
using Newtonsoft.Json;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities;

namespace Smartstore.Core.Security
{
    [SystemName(SystemName)]
    [FriendlyName("Google reCAPTCHA")]
    [Order(0)]
    internal class GoogleRecaptchaProvider : ICaptchaProvider //, IConfigurable
    {
        internal const string SystemName = "Captcha.GoogleRecaptcha";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GoogleRecaptchaSettings _settings;

        public GoogleRecaptchaProvider(IHttpClientFactory httpClientFactory, GoogleRecaptchaSettings settings, CaptchaSettings legacySettings)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings;

            // TEMP only
            _settings.SecretKey = legacySettings.ReCaptchaPrivateKey;
            _settings.SiteKey = legacySettings.ReCaptchaPublicKey;
            _settings.VerifyUrl = EngineContext.Current.Application.AppConfiguration.Google.RecaptchaVerifyUrl;
            _settings.WidgetUrl = EngineContext.Current.Application.AppConfiguration.Google.RecaptchaWidgetUrl;
            if (legacySettings.UseInvisibleReCaptcha)
            {
                _settings.Size = "invisible";
            }
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public bool IsConfigured => _settings.SiteKey.HasValue() && _settings.SecretKey.HasValue();

        public bool IsInvisible => _settings.Size == "invisible";

        public Task<Widget> CreateWidgetAsync(CaptchaContext context)
        {
            Guard.NotNull(context);

            if (!IsConfigured)
            {
                return Task.FromResult<Widget>(null);
            }

            var ident = CommonHelper.GenerateRandomDigitCode(5);
            var elementId = "recaptcha" + ident;
            var callbackName = "recaptchaOnload" + ident;
            var url = "{0}?onload={1}&render=explicit&hl={2}".FormatInvariant(
                _settings.WidgetUrl,
                callbackName,
                context.Language?.UniqueSeoCode.EmptyNull().ToLowerInvariant());

            var script = new[]
            {
                "<script>",
                "   var {0} = function() {{".FormatInvariant(callbackName),
                "       renderGoogleRecaptcha('{0}', '{1}', {2});".FormatInvariant(elementId, _settings.SiteKey, IsInvisible.ToString().ToLowerInvariant()),
                "   };",
                "</script>",
                $"<div id='{elementId}' class='g-recaptcha' data-sitekey='{_settings.SiteKey}' data-theme='{_settings.Theme}' data-size='{_settings.Size}'></div>",
                "<script src='{0}' async defer></script>".FormatInvariant(url)
            }.StrJoin(string.Empty);

            return Task.FromResult<Widget>(new HtmlWidget(script));
        }

        public async Task<CaptchaValidationResult> ValidateAsync(CaptchaContext context, CancellationToken cancelToken = default)
        {
            Guard.NotNull(context);

            var result = new CaptchaValidationResult();

            if (!IsConfigured)
            {
                result.Messages.Add(new CaptchaValidationMessage("configuration-missing", CaptchaValidationMessageLevel.Error));
                return result;
            }

            var token = context.HttpContext.Request.Form["g-recaptcha-response"].ToString();

            if (token.IsEmpty())
            {
                result.Messages.Add(new CaptchaValidationMessage("missing-input-response", CaptchaValidationMessageLevel.Warning));
                return result;
            }

            var client = _httpClientFactory.CreateClient();

            var url = "{0}?secret={1}&response={2}".FormatInvariant(
                _settings.VerifyUrl,
                _settings.SecretKey.UrlEncode(),
                token.UrlEncode());

            var response = await client.GetAsync(url, cancelToken);
            var json = await response.Content.ReadAsStringAsync(cancelToken);

            GoogleRecaptchaApiResponse payload = null;

            try
            {
                payload = JsonConvert.DeserializeObject<GoogleRecaptchaApiResponse>(json);
            }
            catch
            {
            }

            if (payload == null)
            {
                result.Messages.Add(new CaptchaValidationMessage("unable-to-verify", CaptchaValidationMessageLevel.Error));
                return result;
            }

            if (payload.Success)
            {
                result.Success = true;
                return result;
            }

            if (payload.ErrorCodes != null)
            {
                // Do not log 'missing input'. Could be a regular case.
                foreach (var code in payload.ErrorCodes.Where(x => x.HasValue() && x != "missing-input-response"))
                {
                    if (code.IsEmpty())
                    {
                        continue;
                    }

                    var level = code == "invalid-input-response"
                        ? CaptchaValidationMessageLevel.Warning
                        : CaptchaValidationMessageLevel.Error;

                    result.Messages.Add(new CaptchaValidationMessage(code, level));
                }
            }

            return result;
        }
    }

    internal sealed class GoogleRecaptchaApiResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("error-codes")]
        public List<string> ErrorCodes { get; set; }
    }
}
