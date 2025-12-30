//using System.Text.Json;
//using System.Text.Json.Serialization.Metadata;
//using Microsoft.Extensions.Options;
//using Smartstore.Json;
//using Swashbuckle.AspNetCore.SwaggerGen;

//namespace Smartstore.Web.Api.Swagger;

//internal sealed class SmartSerializerDataContractResolver(IOptions<JsonOptions> mvcJsonOptions) : ISerializerDataContractResolver
//{
//    private readonly IOptions<JsonOptions> _mvcJsonOptions = mvcJsonOptions ?? throw new ArgumentNullException(nameof(mvcJsonOptions));

//    public DataContract GetDataContractForType(Type type)
//    {
//        ArgumentNullException.ThrowIfNull(type);

//        // Clone MVC serializer options (includes ApplyFrom(SmartJsonOptions.Default), converters, etc.)
//        var options = new JsonSerializerOptions(_mvcJsonOptions.Value.JsonSerializerOptions);
//        options.ApplyFrom(SmartJsonOptions.Default);

//        options.TypeInfoResolver =
//            (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
//            .WithDataContractModifiers();

//        var inner = new JsonSerializerDataContractResolver(options);

//        //var inner = new JsonSerializerDataContractResolver(_mvcJsonOptions.Value.JsonSerializerOptions);
//        return inner.GetDataContractForType(Nullable.GetUnderlyingType(type) ?? type);
//    }
//}
