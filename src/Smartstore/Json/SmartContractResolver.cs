#nullable enable

using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using STJ = System.Text.Json.Serialization;

namespace Smartstore.Json;

public class SmartContractResolver : DefaultContractResolver
{
    public static SmartContractResolver Default { get; } = new SmartContractResolver(false);
    public static SmartContractResolver CamelCased { get; } = new SmartContractResolver(true);

    public SmartContractResolver(bool camelCased = false)
    {
        if (camelCased)
        {
            NamingStrategy = new CamelCaseNamingStrategy
            {
                OverrideSpecifiedNames = false
            };
        }
    }

    protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
    {
        // .NET 7+ native reflection is ultra-fast, even faster than
        // Newtonsoft's ExpressionValueProvider. As long as the devs
        // does not refactor their code, we gonna return ReflectionValueProvider here.
        return new ReflectionValueProvider(member);
    }

    protected override JsonObjectContract CreateObjectContract(Type objectType)
    {
        var contract = base.CreateObjectContract(objectType);

        // NSJ already supports [Newtonsoft.Json.JsonConstructor] via base implementation.
        // Only add STJ support if NSJ didn't already pick an override creator.
        if (contract.OverrideCreator != null)
            return contract;

        var ctor = FindSystemTextJsonConstructor(objectType);
        if (ctor == null)
            return contract;

        contract.OverrideCreator = args => ctor.Invoke(args);
        contract.CreatorParameters.Clear();
        foreach (var p in CreateConstructorParameters(ctor, contract.Properties))
            contract.CreatorParameters.Add(p);

        return contract;
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        // Apply System.Text.Json attributes if any
        ApplySystemTextJsonAttributes(member, property);

        return property;
    }

    private static ConstructorInfo? FindSystemTextJsonConstructor(Type t)
    {
        var ctors = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // Prefer explicit annotation on ctor
        var marked = ctors.Where(c => c.IsDefined(typeof(STJ.JsonConstructorAttribute), inherit: true)).ToArray();
        if (marked.Length == 0)
            return null;

        if (marked.Length > 1)
            throw new JsonSerializationException(
                $"Multiple constructors on '{t.FullName}' are marked with [{nameof(STJ.JsonConstructorAttribute)}].");

        return marked[0];
    }

    /// <summary>
    /// Applies System.Text.Json-compatible attributes found on the specified member to the given JsonProperty,
    /// updating its serialization behavior accordingly.
    /// </summary>
    /// <remarks>This method inspects the provided member for System.Text.Json attributes, including
    /// JsonPropertyNameAttribute, JsonPropertyOrderAttribute, JsonIgnoreAttribute, JsonIncludeAttribute, and
    /// JsonRequiredAttribute. If present, these attributes are used to update the corresponding settings on the
    /// JsonProperty instance, such as property name, order, ignore conditions, inclusion of non-public members, and
    /// required status. Existing Newtonsoft.Json attributes on the member take precedence and are not overridden.
    /// This method is intended for scenarios where interoperability between Newtonsoft.Json and System.Text.Json
    /// attributes is required.</remarks>
    private static void ApplySystemTextJsonAttributes(MemberInfo member, JsonProperty property)
    {
        var nsjProp = member.GetCustomAttribute<JsonPropertyAttribute>(inherit: true);

        // If Newtonsoft already treated this as "explicitly configured" (e.g. [JsonProperty]),
        // do not override the name. This keeps the hybrid phase predictable.
        if (nsjProp == null || nsjProp.PropertyName.IsEmpty())
        {
            // [JsonPropertyName] -> force exact name (wins over NamingStrategy because we assign last)
            if (member.TryGetAttribute<STJ.JsonPropertyNameAttribute>(true, out var attr) && attr.Name.HasValue())
            {
                property.PropertyName = attr.Name;
                property.HasMemberAttribute = true;
            }
        }

        // [JsonPropertyOrder] -> force order (wins over NamingStrategy because we assign last)
        if (nsjProp == null || property.Order == null)
        {
            if (member.TryGetAttribute<STJ.JsonPropertyOrderAttribute>(true, out var attr))
            {
                property.Order = attr.Order;
                property.HasMemberAttribute = true;
            }
        }

        // [JsonIgnore] --> prefer over [IgnoreDataMember]
        if (member.TryGetAttribute<STJ.JsonIgnoreAttribute>(true, out var stjIgnore))
        {
            // In STJ default is "Always".
            switch (stjIgnore.Condition)
            {
                case STJ.JsonIgnoreCondition.Always:
                    property.Ignored = true;
                    break;

                case STJ.JsonIgnoreCondition.WhenWritingNull:
                    property.NullValueHandling ??= NullValueHandling.Ignore;
                    break;

                case STJ.JsonIgnoreCondition.WhenWritingDefault:
                    property.DefaultValueHandling ??= DefaultValueHandling.Ignore;
                    break;

                case STJ.JsonIgnoreCondition.WhenWriting:
                    property.ShouldSerialize = _ => false;
                    break;

                case STJ.JsonIgnoreCondition.WhenReading:
                    property.ShouldDeserialize = _ => false;
                    break;

                case STJ.JsonIgnoreCondition.Never:
                default:
                    break;
            }
        }

        // [JsonInclude] -> allow non-public getter/setter/field
        if (member.IsDefined(typeof(STJ.JsonIncludeAttribute), inherit: true))
        {
            if (member is PropertyInfo pi)
            {
                if (pi.GetGetMethod(nonPublic: true) != null) property.Readable = true;
                if (pi.GetSetMethod(nonPublic: true) != null) property.Writable = true;
            }
            else if (member is FieldInfo)
            {
                property.Readable = true;
                property.Writable = true;
            }
        }

        // [JsonRequired] (STJ, .NET 7+)
        if (member.IsDefined(typeof(STJ.JsonRequiredAttribute), inherit: true))
        {
            property.Required = Required.Always;
        }
    }
}
