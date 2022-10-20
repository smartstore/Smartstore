using Microsoft.AspNetCore.OData.Formatter;

namespace Smartstore.Web.Api
{
    public static class ODataActionParametersExtensions
    {
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
