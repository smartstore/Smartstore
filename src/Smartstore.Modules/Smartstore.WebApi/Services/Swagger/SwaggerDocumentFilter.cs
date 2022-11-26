using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartstore.Web.Api.Swagger
{
    /// <summary>
    /// Removes Open API paths and schemas that should not be displayed in the Swagger documentation.
    /// </summary>
    /// <remarks>
    /// It would be nice if we could strip off the OData route here: /odata/v1/categories({key}) -> /categories({key})
    /// but that would let Swagger execute against /categories({key}) always resulting in 404 NotFound.
    /// To achieve this a custom Swagger template would be required. Perhaps there is an extension somewhere.
    /// </remarks>
    internal partial class SwaggerDocumentFilter : IDocumentFilter
    {
        [GeneratedRegex("[a-z0-9\\/](\\$count|\\{key\\}|default\\.)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, "de-DE")]
        private static partial Regex PathsToIgnoreRegex();

        private static readonly Regex _pathsToIgnore = PathsToIgnoreRegex();

        //private static readonly Regex _schemasToIgnore = new(@"(Microsoft\.AspNetCore\.OData\.|System\.Collections\.Generic\.KeyValuePair).+",
        //    RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Type[] _schemaTypesToRemove = new[] { typeof(SingleResult) };

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            try
            {
                FilterSchemas(context);
                FilterPaths(swaggerDoc, context);
            }
            catch (Exception ex)
            {
                ex.Dump();
            }
        }

        /// <summary>
        /// Removes unexpected, unwanted schemas.
        /// </summary>
        private static void FilterSchemas(DocumentFilterContext context)
        {
            foreach (var schema in context.SchemaRepository.Schemas)
            {
                if (_schemaTypesToRemove.Any(type => schema.Key.StartsWithNoCase(type.FullName)))
                {
                    context.SchemaRepository.Schemas.Remove(schema.Key);
                }
            }
        }

        /// <summary>
        /// Removes duplicate and unusual, unexpected documents.
        /// </summary>
        private static void FilterPaths(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var apiDescription = context?.ApiDescriptions?.FirstOrDefault();
            var routeTemplate = apiDescription?.ActionDescriptor?.AttributeRouteInfo?.Template?.EnsureStartsWith('/');

            foreach (var item in swaggerDoc.Paths)
            {
                var path = item.Key;
                var removePath = path.EqualsNoCase(routeTemplate) || _pathsToIgnore.IsMatch(path);
                if (removePath)
                {
                    swaggerDoc.Paths.Remove(path);
                }
                else if (path.EndsWith("()") && swaggerDoc.Paths.Any(x => x.Key == path[..^2]))
                {
                    // Duplicate.
                    swaggerDoc.Paths.Remove(path);
                }

                //$"{removePath} {item.Key} {string.Join(",", item.Value.Operations.Select(x => x.Key))}".Dump();
            }
        }
    }
}
