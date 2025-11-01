using System.Net.Http;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Utilities;

namespace Smartstore.Core.Security
{
    [SystemName(SystemName)]
    [FriendlyName("Google reCAPTCHA")]
    [Order(0)]
    internal class GoogleRecaptchaProvider : ICaptchaProvider, IConfigurable
    {
        internal const string SystemName = "Captcha.GoogleRecaptcha";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GoogleRecaptchaSettings _settings;

        public GoogleRecaptchaProvider(IHttpClientFactory httpClientFactory, GoogleRecaptchaSettings settings)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public bool IsConfigured 
            => _settings.SiteKey.HasValue() && _settings.SecretKey.HasValue();

        public bool IsInvisible 
            => IsV2 && _settings.Size.EqualsNoCase("invisible");

        public bool IsNonInteractive => 
            IsV3 || IsInvisible;

        public RouteInfo GetConfigurationRoute()
            => new("GoogleRecaptcha", "Security", new { area = "Admin" });

        public Task<Widget> CreateWidgetAsync(CaptchaContext context)
        {
            Guard.NotNull(context);

            if (!IsConfigured)
            {
                return Task.FromResult<Widget>(null);
            }

            var isV3 = IsV3;
            var theme = _settings.UseDarkTheme ? "dark" : "light";
            var lang = context.Language?.UniqueSeoCode.EmptyNull().ToLowerInvariant();
            var baseUrl = _settings.WidgetUrl.NullEmpty() ?? GoogleRecaptchaSettings.DefaultWidgetUrl;
            var apiSrc = isV3 ? $"{baseUrl}?render={_settings.SiteKey}{(lang.HasValue() ? "&hl=" + lang : string.Empty)}"
                              : $"{baseUrl}?render=explicit{(lang.HasValue() ? "&hl=" + lang : string.Empty)}";

            var assetBuilder = context.AssetBuilder;

            // Register reCAPTCHA external API script
            var script = $"<script src='{apiSrc}' async defer></script>";
            assetBuilder.AddHtmlContent("scripts", new HtmlString(script), apiSrc);

            // Register required reCAPTCHA bootstrapper
            var scriptUrl = "/js/smartstore.captcha.grecaptcha.js";
            script = $"<script src='{context.Url.Content(scriptUrl)}' async defer></script>";
            assetBuilder.AddHtmlContent("scripts", new HtmlString(script), scriptUrl);

            var isHiddenBadge = (!isV3 && _settings.Size.EqualsNoCase("invisible") && _settings.BadgePosition.EqualsNoCase("hide"))
                || (isV3 && _settings.HideBadgeV3);

            var content = new HtmlContentBuilder();

            if (!isV3)
            {
                // v2: render explicit widget placeholder
                var elementId = "recaptcha" + CommonHelper.GenerateRandomDigitCode(5);
                
                var element = new TagBuilder("div");
                element.Attributes["id"] = elementId;
                element.Attributes["data-sitekey"] = _settings.SiteKey;
                element.Attributes["data-theme"] = theme;
                element.Attributes["data-size"] = _settings.Size;
                element.Attributes["class"] = "g-recaptcha";

                if (isHiddenBadge)
                {
                    // Invisible badge: we need to render the legally required notice here.
                    element.InnerHtml.AppendHtml(T("Admin.Configuration.Settings.GeneralCommon.GoogleRecaptcha.HiddenBadgeLegalNotice").Value);
                }

                content.AppendLine(element);

                // Provide minimal JSON config for the adapter
                var config = new
                {
                    siteKey = _settings.SiteKey,
                    size = _settings.Size,
                    badge = _settings.BadgePosition.EqualsNoCase("hide") ? "inline" : _settings.BadgePosition,
                    theme,
                    elementId
                };
                content.AppendHtmlLine($"<script type='application/json' class='captcha-config'>{JsonConvert.SerializeObject(config)}</script>");
            }
            else
            {
                // v3: no visible widget; ensure hidden response field exists and expose config
                content.AppendHtmlLine("<input type='hidden' id='g-recaptcha-response' name='g-recaptcha-response' value='' />");

                if (isHiddenBadge)
                {
                    // Invisible badge: we need to render the legally required notice here.
                    content.AppendHtmlLine(T("Admin.Configuration.Settings.GeneralCommon.GoogleRecaptcha.HiddenBadgeLegalNotice").Value);
                }

                // Provide minimal JSON config for the adapter
                var cfg = new
                {
                    siteKey = _settings.SiteKey,
                    defaultAction = _settings.DefaultAction,
                    scoreThreshold = _settings.ScoreThreshold,
                    hideBadge = _settings.HideBadgeV3,
                };
                content.AppendHtmlLine($"<script type='application/json' class='captcha-config'>{JsonConvert.SerializeObject(cfg)}</script>");
            }

            if (isHiddenBadge)
            {
                context.AssetBuilder.BodyAttributes.AddInValue("class", ' ', "grecaptcha-badge-hidden", false);
            }

            return Task.FromResult<Widget>(new HtmlWidget(content));
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
            client.Timeout = TimeSpan.FromSeconds(10);

            var verifyUrl = _settings.VerifyUrl.NullEmpty() ?? GoogleRecaptchaSettings.DefaultVerifyUrl;

            using var content = new FormUrlEncodedContent(
[
                new KeyValuePair<string, string>("secret", _settings.SecretKey),
                new KeyValuePair<string, string>("response", token),
                new KeyValuePair<string, string>("remoteip", context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty)
            ]);


            GoogleRecaptchaApiResponse payload = null;

            try
            {
                using var response = await client.PostAsync(verifyUrl, content, cancelToken);
                var json = await response.Content.ReadAsStringAsync(cancelToken);
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

            if (!payload.Success)
            {
                if (payload.ErrorCodes != null)
                {
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

            // v3 extra checks ---
            if (IsV3)
            {
                var score = payload.Score ?? 0f;
                if (score < _settings.ScoreThreshold)
                {
                    result.Messages.Add(new CaptchaValidationMessage($"low-score({score:0.00})", CaptchaValidationMessageLevel.Warning));
                    return result;
                }

                // 2) Optional action comparison
                // Provider-agnostic convention: read "captcha-action" from POST. If not present, fall back to DefaultAction.
                var postedAction = context.HttpContext.Request.Form["captcha-action"].ToString();
                var expectedAction = postedAction.NullEmpty() ?? _settings.DefaultAction;

                if (!string.IsNullOrEmpty(expectedAction) && !expectedAction.EqualsNoCase(payload.Action))
                {
                    result.Messages.Add(new CaptchaValidationMessage("action-mismatch", CaptchaValidationMessageLevel.Warning));
                    return result;
                }
            }

            result.Success = true;
            return result;
        }

        private bool IsV3 => _settings.Version.Equals("v3", StringComparison.OrdinalIgnoreCase);
        private bool IsV2 => _settings.Version.Equals("v2", StringComparison.OrdinalIgnoreCase);
    }

    internal sealed class GoogleRecaptchaApiResponse
    {
        [JsonProperty("success")] 
        public bool Success { get; set; }

        [JsonProperty("score")] 
        public float? Score { get; set; } // v3

        [JsonProperty("action")] 
        public string Action { get; set; } // v3

        [JsonProperty("challenge_ts")] 
        public DateTime? ChallengeTs { get; set; } // v2/v3

        [JsonProperty("hostname")] 
        public string Hostname { get; set; } // v2/v3

        [JsonProperty("error-codes")] 
        public List<string> ErrorCodes { get; set; }
    }
}
