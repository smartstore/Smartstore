using System.Security.Cryptography;
using DotLiquid;
using Humanizer;
using Newtonsoft.Json;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;
using Smartstore.Utilities;

namespace Smartstore.Templating.Liquid
{
    public static class AdditionalFilters
    {
        private static LiquidTemplateEngine GetEngine()
        {
            return (LiquidTemplateEngine)Template.FileSystem;
        }

        #region String Filters

        public static string Prettify(object input, bool allowSpace)
        {
            if (ConvertUtility.TryConvert<long>(input, out var l))
            {
                return Prettifier.HumanizeBytes(l);
            }
            else if (input is string s)
            {
                return SlugUtility.Slugify(s, new SlugifyOptions { AllowSpace = allowSpace });
            }

            return null;
        }

        public static string SanitizeHtmlId(string input)
        {
            return input?.SanitizeHtmlId();
        }

        public static string Pluralize(string input)
        {
            return input.EmptyNull().Pluralize();
        }

        public static string Titleize(string input)
        {
            return input.EmptyNull().Titleize();
        }

        public static string Md5(string input)
        {
            if (input == null)
                return input;

            return MD5.HashData(input.GetBytes()).ToHexString();
        }

        #endregion

        #region Html Filters

        public static string ScriptTag(string input)
        {
            return string.Format("<script src=\"{0}\"></script>", input);
        }

        public static string StylesheetTag(string input)
        {
            return string.Format("<link href=\"{0}\" rel=\"stylesheet\" type=\"text/css\" media=\"all\" />", input);
        }

        public static string ImgTag(string input, string alt = "", string css = "")
        {
            return input == null ? null : GetImageTag(input, alt, css);
        }

        private static string GetImageTag(string src, string alt, string css)
        {
            return string.Format("<img src=\"{0}\" alt=\"{1}\" class=\"{2}\" />", src, alt, css);
        }

        #endregion

        #region Localization Filters

        public static string T(Context context, string key, object arg1 = null, object arg2 = null, object arg3 = null, object arg4 = null)
        {
            var engine = GetEngine();

            int languageId = 0;

            if (context["Context.LanguageId"] is int lid)
            {
                languageId = lid;
            }

            var args = (new object[] { arg1, arg2, arg3, arg4 }).ToArray();

            return engine.T(key, languageId, args);
        }

        #endregion

        #region Common Filters

        public static string Json(object input)
        {
            if (input == null)
                return null;

            return JsonConvert.SerializeObject(input, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        }

        public static string FormatAddress(Context context, object input)
        {
            if (input == null)
                return null;

            var db = EngineContext.Current.ResolveService<SmartDbContext>();
            var addressService = EngineContext.Current.ResolveService<IAddressService>();

            Country country = null;

            // We know that we converted Address entity to a dictionary.

            if (input is IDictionary<string, object> dict)
            {
                country = db.Countries.FindById(dict.Get("CountryId").Convert<int?>() ?? 0, false);
            }
            else if (input is IIndexable lq)
            {
                country = db.Countries.FindById(lq["CountryId"].Convert<int?>() ?? 0, false);
            }

            return addressService.FormatAddressAsync(input, country?.AddressFormat, context.FormatProvider).Await();
        }

        #endregion
    }
}
