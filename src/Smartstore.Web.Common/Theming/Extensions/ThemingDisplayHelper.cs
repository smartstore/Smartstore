using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Smartstore.Web.Theming;

namespace Smartstore
{
    public static class ThemingDisplayHelper
    {
        /// <summary>
        /// Gets the manifest of the current active theme
        /// </summary>
        public static ThemeManifest GetThemeManifest(this IDisplayHelper displayHelper)
        {
            return displayHelper.Resolve<IThemeContext>().CurrentTheme;
        }

        /// <summary>
        /// Gets the runtime theme variables as specified in the theme's config file
        /// alongside the merged user-defined variables
        /// </summary>
        public static dynamic GetThemeVariables(this IDisplayHelper displayHelper)
        {
            return displayHelper.HttpContext.GetItem("ThemeVariables", () =>
            {
                // TODO: (core) Implement ThemingDisplayHelper.GetThemeVariables()
                return new ExpandoObject();
            });
        }

        /// <summary>
        /// Gets a runtime theme variable value
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <param name="defaultValue">The default value to return if the variable does not exist</param>
        /// <returns>The theme variable value</returns>
        public static T GetThemeVariable<T>(this IDisplayHelper displayHelper, string name, T defaultValue = default)
        {
            Guard.NotEmpty(name, nameof(name));

            var vars = GetThemeVariables(displayHelper) as IDictionary<string, object>;
            if (vars != null && vars.ContainsKey(name))
            {
                string value = vars[name] as string;
                if (!value.HasValue())
                {
                    return defaultValue;
                }

                return value.Convert<T>();
            }

            return defaultValue;
        }
    }
}
