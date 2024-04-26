using System.Text;
using System.Text.RegularExpressions;
using Humanizer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Smartstore.Web.Api.Swagger
{
    internal static partial class OpenApiUtility
    {
        [GeneratedRegex(@"[{}\$]", RegexOptions.Compiled | RegexOptions.Singleline)]
        private static partial Regex InvalidOperationIdCharsRegex();
        private static readonly Regex InvalidOperationIdChars = InvalidOperationIdCharsRegex();

        /// <summary>
        /// Gets a humanized name for one of the names of <see cref="WebApiGroupNames"/>.
        /// </summary>
        public static string GetDocumentName(string name)
            => name.Replace('-', ' ').Titleize();

        /// <summary>
        /// Gets a unique and valid OpenAPI Operation-ID. By default <see cref="ApiDescription.RelativePath"/> is used but it is not unique.
        /// Some OpenAPI validators expect URL friendly characters.
        /// </summary>
        /// <remarks>
        /// Also prevents multiple descriptions from expanding at the same time when clicking a method in Swagger UI.
        /// </remarks>
        public static string GetOperationId(ApiDescription description)
        {
            var path = InvalidOperationIdChars.Replace(
                description.RelativePath.Replace("odata/", string.Empty).Replace('/', '-'),
                string.Empty);

            return description.HttpMethod.ToLower().Grow(path, "-");
        }

        /// <summary>
        /// Gets a unique OpenAPI schema ID for an object type.
        /// Avoids "Conflicting schemaIds" (multiple types with the same name but different namespaces).
        /// </summary>
        public static string GetSchemaId(Type type, bool fullyQualified = false)
        {
            Guard.NotNull(type);

            var typeName = fullyQualified
                ? GetFullNameSansTypeParameters().Replace('+', '.')
                : type.Name;

            if (type.IsGenericType)
            {
                var genericArgumentIds = type.GetGenericArguments()
                    .Select(t => GetSchemaId(t, fullyQualified))
                    .ToArray();

                return new StringBuilder(typeName)
                    .Replace(string.Format("`{0}", genericArgumentIds.Length), string.Empty)
                    .Append(string.Format("[{0}]", string.Join(',', genericArgumentIds).TrimEnd(',')))
                    .ToString();
            }

            return typeName;

            string GetFullNameSansTypeParameters()
            {
                var fullName = type.FullName.NullEmpty() ?? type.Name;
                var chopIndex = fullName.IndexOf("[[");

                return (chopIndex == -1) ? fullName : fullName[..chopIndex];
            }
        }
    }
}
