#nullable enable

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartstore.Web.Api.Swagger;

internal sealed class SmartSerializerDataContractResolver : ISerializerDataContractResolver
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly JsonSerializerDataContractResolver _innerResolver;

    public SmartSerializerDataContractResolver(IOptions<JsonOptions> jsonOptions)
    {
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        _innerResolver = new JsonSerializerDataContractResolver(_jsonOptions);
    }

    public DataContract GetDataContractForType(Type type)
    {
        var contract = _innerResolver.GetDataContractForType(type);

        if (contract.DataType is DataType.Object)
        {
            JsonTypeInfo? typeInfo = null;
            try
            {
                typeInfo = _jsonOptions.GetTypeInfo(contract.UnderlyingType);
            }
            catch
            {
            }

            // Only take over object contracts; everything else stays with Swashbuckle.
            if (typeInfo != null && typeInfo.Kind == JsonTypeInfoKind.Object)
            {
                var jsonProps = typeInfo.Properties.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
                var actualProps = new List<DataProperty>();

                foreach (var prop in contract.ObjectProperties)
                {
                    if (jsonProps.TryGetValue(prop.Name, out var jp))
                    {
                        var isIgnored = jp.Get is null && jp.Set is null;
                        if (isIgnored) continue;
                        
                        var isRequired = jp.IsRequired;
                        var isNullable = !jp.PropertyType.IsValueType || jp.IsGetNullable;
                        var isReadOnly = jp.Get is not null && jp.Set is null;
                        var isWriteOnly = jp.Get is null && jp.Set is not null;
                        var name = prop.Name;

                        actualProps.Add(new DataProperty(
                            name: prop.Name,
                            memberType: prop.MemberType,
                            memberInfo: prop.MemberInfo,
                            isRequired: isRequired,
                            isNullable: isNullable,
                            isReadOnly: isReadOnly,
                            isWriteOnly: isWriteOnly));
                    }
                }

                contract = DataContract.ForObject(
                    underlyingType: contract.UnderlyingType,
                    properties: actualProps,
                    extensionDataType: contract.ObjectExtensionDataType,
                    typeNameProperty: contract.ObjectTypeNameProperty,
                    typeNameValue: contract.ObjectTypeNameValue,
                    jsonConverter: contract.JsonConverter);
            }
        }

        return contract;
    }
}
