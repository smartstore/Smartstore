//using Asp.Versioning.ApiExplorer;
//using Microsoft.Extensions.Options;
//using Microsoft.OpenApi.Models;
//using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartstore.Web.Api.Swagger
{
    /// <summary>
    /// Configures the Swagger generation options.
    /// </summary>
    /// <remarks>
    /// This allows API versioning to define a Swagger document per API version after the
    /// <see cref="IApiVersionDescriptionProvider"/> service has been resolved from the service container.
    /// </remarks>
    //public class SwaggerGenOptionsConfiguration : IConfigureOptions<SwaggerGenOptions>
    //{
    //    private readonly IApiVersionDescriptionProvider provider;

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="ConfigureSwaggerOptions"/> class.
    //    /// </summary>
    //    /// <param name="provider">The <see cref="IApiVersionDescriptionProvider">provider</see> used to generate Swagger documents.</param>
    //    public SwaggerGenOptionsConfiguration(IApiVersionDescriptionProvider provider) 
    //        => this.provider = provider;

    //    /// <inheritdoc />
    //    public void Configure(SwaggerGenOptions options)
    //    {
    //        // Add a swagger document for each discovered API version.
    //        // Note: you might choose to skip or document deprecated API versions differently.
    //        foreach (var description in provider.ApiVersionDescriptions)
    //        {
    //            options.SwaggerDoc(description.GroupName/* 1.0 */, CreateInfoForApiVersion(description));
    //        }
    //    }

    //    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    //    {
    //        var info = new OpenApiInfo
    //        {
    //            Title = "Smartstore Web API",
    //            Version = description.ApiVersion.ToString() // 1.0
    //        };

    //        $"group {description.GroupName}, version {info.Version}".Dump();

    //        if (description.IsDeprecated)
    //        {
    //            info.Description = "This API version has been deprecated.";
    //        }

    //        return info;
    //    }
    //}
}
