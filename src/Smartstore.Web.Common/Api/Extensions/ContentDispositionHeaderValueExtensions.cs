using System.Net.Http.Headers;

namespace Smartstore.Web.Api
{
    public static class ContentDispositionHeaderValueExtensions
    {
        /// <summary>
        /// Gets the value of a content disposition parameter.
        /// Removes quotation marks at beginning and end if present.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="defaultValue">The default value (fallback).</param>
        /// <returns>Converted content disposition parameter value.</returns>
        /// <remarks>
        /// Parameter values can send quoted, unquoted and mime encoded (UTF-8). It is recommended to exchange quoted values to avoid 
        /// <see cref="InvalidDataException"/> when using <see cref="ContentDispositionHeaderValue.Parse"/>.
        /// </remarks>
        public static T GetParameterValue<T>(this ContentDispositionHeaderValue value, string name, T defaultValue = default)
        {
            Guard.NotEmpty(name, nameof(name));

            var str = value?.Parameters?.FirstOrDefault(x => x.Name.EqualsNoCase(name))?.Value?.Trim('"');
            if (str != null)
            {
                return str.Convert(defaultValue);
            }

            return defaultValue;
        }
    }
}
