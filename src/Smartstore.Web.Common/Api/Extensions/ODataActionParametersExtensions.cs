using Microsoft.AspNetCore.OData.Formatter;

namespace Smartstore.Web.Api
{
    public static class ODataActionParametersExtensions
    {
        /// <summary>
        /// Gets a value from <see cref="ODataActionParameters"/>.
        /// </summary>
        /// <param name="key">The parameter key.</param>
        /// <param name="defaultValue">The default value (fallback).</param>
        /// <returns>Converted action parameter value.</returns>
        public static T GetValueSafe<T>(this ODataActionParameters parameters, string key, T defaultValue = default)
        {
            if (parameters != null && key.HasValue() && parameters.TryGetValue(key, out var value))
            {
                return value.Convert(defaultValue);
            }

            return defaultValue;
        }
    }
}
