using System.Dynamic;
using Smartstore.Core.Stores;
using Smartstore.Core.Theming;
using Smartstore.Web.Theming;

namespace Smartstore
{
    public static class ThemingDisplayHelper
    {
        /// <summary>
        /// Gets the descriptor of the current active theme
        /// </summary>
        public static ThemeDescriptor GetThemeDescriptor(this IDisplayHelper displayHelper)
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
                var services = displayHelper.HttpContext.RequestServices;
                var storeContext = services.GetService<IStoreContext>();
                var themeDescriptor = displayHelper.GetThemeDescriptor();

                if (storeContext == null || themeDescriptor == null)
                {
                    return new ExpandoObject();
                }
                else
                {
                    var repo = services.GetService<ThemeVariableRepository>();
                    return repo.GetRawVariablesAsync(themeDescriptor.Name, storeContext.CurrentStore.Id).Await();
                }
            });
        }

        /// <summary>
        /// Gets a runtime theme variable value as string.
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <param name="defaultValue">The default value to return if the variable does not exist</param>
        /// <returns>The theme variable value</returns>
        public static string GetThemeVariable(this IDisplayHelper displayHelper, string name, string defaultValue = "")
            => GetThemeVariable<string>(displayHelper, name, defaultValue);

        /// <summary>
        /// Gets a runtime theme variable value
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <param name="defaultValue">The default value to return if the variable does not exist</param>
        /// <returns>The theme variable value</returns>
        public static T GetThemeVariable<T>(this IDisplayHelper displayHelper, string name, T defaultValue = default)
        {
            Guard.NotEmpty(name);

            if (GetThemeVariables(displayHelper) is IDictionary<string, object> vars)
            {
                if (vars.TryGetValueAs<string>(name, out var value) && value.HasValue())
                {
                    return value.Convert(defaultValue);
                }
            }

            return defaultValue;
        }
    }
}
