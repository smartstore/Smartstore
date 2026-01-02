#nullable enable

using System.Runtime.Serialization;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartstore.Web.Api.Swagger;

internal sealed class SmartSerializerDataContractResolver : ISerializerDataContractResolver
{
    private readonly JsonSerializerDataContractResolver _innerResolver;

    public SmartSerializerDataContractResolver(IOptions<JsonOptions> jsonOptions)
    {
        _innerResolver = new JsonSerializerDataContractResolver(jsonOptions.Value.JsonSerializerOptions);
    }

    public DataContract GetDataContractForType(Type type)
    {
        var contract = _innerResolver.GetDataContractForType(type);

        if (contract.DataType is DataType.Object)
        {
            // The original SerializerDataContractResolver does not respect [IgnoreDataMember], so we need to filter them out ourselves.
            var ignoredProperties = contract.ObjectProperties
                .Where(p => p.MemberInfo.HasAttribute<IgnoreDataMemberAttribute>(true))
                .ToArray();

            if (ignoredProperties.Length > 0)
            {
                // There are ignored properties; filter them out.
                contract = DataContract.ForObject(
                    underlyingType: contract.UnderlyingType,
                    properties: contract.ObjectProperties.Except(ignoredProperties).ToArray(),
                    extensionDataType: contract.ObjectExtensionDataType,
                    typeNameProperty: contract.ObjectTypeNameProperty,
                    typeNameValue: contract.ObjectTypeNameValue,
                    jsonConverter: contract.JsonConverter);
            }
        }

        return contract;
    }
}
