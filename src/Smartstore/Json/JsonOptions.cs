#nullable enable

using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Smartstore.Json;

/// <summary>
/// Provides preconfigured and utility options for System.Text.Json serialization that closely match Newtonsoft.Json
/// (Json.NET) defaults, along with helpers for customizing and applying JSON serializer settings.
/// </summary>
/// <remarks>The SmartJsonOptions class supplies ready-to-use JsonSerializerOptions instances, such as Default and
/// CamelCased, that emulate common Newtonsoft.Json behaviors for compatibility and ease of migration. It also includes
/// extension methods for creating, modifying, and applying options, as well as support for DataContract attributes.
/// These options are intended to simplify configuration and ensure consistent JSON serialization across
/// applications.</remarks>
public static class SmartJsonOptions
{
    /// <summary>
    /// The default baseline JsonSerializerOptions configured to mimic Newtonsoft.Json (Json.NET) default behaviors.
    /// </summary>
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.General)
    {
        // NSJ default
        AllowTrailingCommas = true,
        
        // Include public fields (default NSJ behaviour)
        IncludeFields = true,

        // NSJ default
        PropertyNameCaseInsensitive = true,

        // Our previous NSJ naming policy was member-casing (which does nothing)
        PropertyNamingPolicy = null,

        // Cycles become null'd instead of throwing.
        // NSJ: ReferenceLoopHandling.Ignore
        ReferenceHandler = ReferenceHandler.IgnoreCycles,

        // STJ supports a global preference (net8+ / net10 docs shown here).
        // Replace is the closest equivalent to "create new instance instead of populating existing".
        // NSJ: ObjectCreationHandling.Replace
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,

        // NSJ: NullValueHandling.Ignore
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // NSJ: MaxDepth
        MaxDepth = 32,
        
        // NSJ default
        ReadCommentHandling = JsonCommentHandling.Skip,

        // NSJ default
        NumberHandling = JsonNumberHandling.AllowReadingFromString,

        // Create a resolver early to allow adding modifiers
        TypeInfoResolver = (JsonSerializer.IsReflectionEnabledByDefault ? new DefaultJsonTypeInfoResolver() : JsonTypeInfoResolver.Combine())
            .WithDataContractModifiers(),

        Converters =
        {
            // Serialize enums as strings, not as ints.
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true)
        }
    };

    /// <summary>
    /// Gets a preconfigured instance of <see cref="JsonSerializerOptions"/> that uses camel case for property names
    /// during serialization and deserialization.
    /// </summary>
    public static readonly JsonSerializerOptions CamelCased = Default.Create(o =>
    {
        o.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

    /// <summary>
    /// Provides a preconfigured <see cref="JsonSerializerOptions"/> instance that uses camel case property naming and
    /// ignores properties with default values during serialization.
    /// </summary>
    public static readonly JsonSerializerOptions CamelCasedIgnoreDefaultValue = CamelCased.Create(o =>
    {
        o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    });

    #region DataContract support modifiers

    /// <summary>
    /// Returns a JSON type info resolver that applies DataContract-related modifiers, enabling support for DataContract
    /// attributes during serialization and deserialization.
    /// </summary>
    /// <remarks>The returned resolver will honor <see
    /// cref="System.Runtime.Serialization.IgnoreDataMemberAttribute"/> and <see
    /// cref="System.Runtime.Serialization.DataMemberAttribute"/> annotations, allowing for customized property naming,
    /// ordering, and member inclusion based on these attributes.</remarks>
    public static IJsonTypeInfoResolver WithDataContractModifiers(this IJsonTypeInfoResolver typeInfoResolver)
    {
        Guard.NotNull(typeInfoResolver);

        return typeInfoResolver
            .WithAddedModifier(ApplyIgnoreDataMember);
    }

    /// <summary>
    /// Configures the specified type to ignore properties marked with the IgnoreDataMemberAttribute during JSON
    /// serialization and deserialization.
    /// </summary>
    /// <remarks>This method disables both reading and writing for properties decorated with
    /// IgnoreDataMemberAttribute, ensuring they are not included in JSON output or processed during deserialization.
    /// Properties marked as required will also be unset to prevent errors when setters are removed.</remarks>
    /// <param name="typeInfo">The type metadata to apply ignore rules to. Must represent an object type; properties with the
    /// IgnoreDataMemberAttribute will be excluded from serialization and deserialization.</param>
    internal static void ApplyIgnoreDataMember(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var prop in typeInfo.Properties)
        {
            if (prop.AttributeProvider is not MemberInfo mi)
                continue;

            if (!mi.IsDefined(typeof(IgnoreDataMemberAttribute), inherit: true))
                continue;

            // Ignore on write
            prop.Get = null;

            // Ignore on read
            prop.Set = null;

            // Safety: if it was marked required somewhere, required+no-setter can explode.
            prop.IsRequired = false;

            //prop.ShouldSerialize = (_, __) => false;
            //prop.ShouldDeserialize = (_, __) => false;
        }
    }

    ///// <summary>
    ///// Applies the DataMemberAttribute name and order values to the properties of the specified JsonTypeInfo object if
    ///// it represents an object type.
    ///// </summary>
    ///// <remarks>This method sets the Name and Order of each property in the JsonTypeInfo to match the
    ///// corresponding values from the DataMemberAttribute, if present. Only properties backed by a MemberInfo and
    ///// decorated with DataMemberAttribute are affected. Properties without DataMemberAttribute or with negative Order
    ///// values are left unchanged.</remarks>
    ///// <param name="typeInfo">The JsonTypeInfo instance whose properties will be updated based on DataMemberAttribute metadata. Must represent
    ///// an object type; otherwise, no changes are made.</param>
    //internal static void ApplyDataMemberNameAndOrder(JsonTypeInfo typeInfo)
    //{
    //    if (typeInfo.Kind != JsonTypeInfoKind.Object)
    //        return;

    //    foreach (var prop in typeInfo.Properties)
    //    {
    //        if (prop.AttributeProvider is not MemberInfo mi)
    //            continue;

    //        var dm = mi.GetCustomAttribute<DataMemberAttribute>(inherit: true);
    //        if (dm is null)
    //            continue;

    //        if (!string.IsNullOrWhiteSpace(dm.Name))
    //        {
    //            // JsonPropertyInfo.Name is settable
    //            prop.Name = dm.Name;
    //        }

    //        if (dm.Order >= 0)
    //        {
    //            // JsonPropertyInfo.Order is settable
    //            prop.Order = dm.Order;
    //        }
    //    }
    //}

    #endregion

    extension(JsonSerializerOptions options)
    {
        /// <summary>
        /// Creates a new instance of <see cref="JsonSerializerOptions"/> based on the current options, allowing
        /// additional configuration through the specified delegate.
        /// </summary>
        /// <remarks>The returned options are independent of the original options and can be modified
        /// without affecting the source instance.</remarks>
        /// <param name="configure">A delegate that receives the new <see cref="JsonSerializerOptions"/> instance for further customization.
        /// Cannot be null.</param>
        /// <returns>A new <see cref="JsonSerializerOptions"/> instance configured with the specified delegate.</returns>
        public JsonSerializerOptions Create(Action<JsonSerializerOptions> configure)
        {
            Guard.NotNull(options);
            Guard.NotNull(configure);

            var opts = new JsonSerializerOptions(options);
            configure(opts);
            return opts;
        }

        /// <summary>
        /// Applies the current set of JSON serializer options to the specified target <see
        /// cref="JsonSerializerOptions"/> instance.
        /// </summary>
        /// <remarks>This method copies all relevant option values and converters from the current options
        /// to the specified target. Existing converters on the target that match by type are not duplicated. The method
        /// does not create a new <see cref="JsonSerializerOptions"/> instance; it modifies and returns the provided
        /// target instance.</remarks>
        /// <param name="target">The <see cref="JsonSerializerOptions"/> instance to which the options will be applied. Cannot be null.</param>
        /// <returns>The <paramref name="target"/> instance with the updated serializer options applied.</returns>
        public JsonSerializerOptions ApplyTo(JsonSerializerOptions target)
        {
            Guard.NotNull(options);
            Guard.NotNull(target);

            target.AllowOutOfOrderMetadataProperties = options.AllowOutOfOrderMetadataProperties;
            target.AllowTrailingCommas = options.AllowTrailingCommas;
            target.DefaultIgnoreCondition = options.DefaultIgnoreCondition;
            target.DefaultBufferSize = options.DefaultBufferSize;
            target.DictionaryKeyPolicy = options.DictionaryKeyPolicy;
            target.Encoder = options.Encoder;
            target.IgnoreReadOnlyFields = options.IgnoreReadOnlyFields;
            target.IgnoreReadOnlyProperties = options.IgnoreReadOnlyProperties;
            target.IncludeFields = options.IncludeFields;
            target.IndentCharacter = options.IndentCharacter;
            target.IndentSize = options.IndentSize;
            target.MaxDepth = options.MaxDepth;
            target.NewLine = options.NewLine;
            target.NumberHandling = options.NumberHandling;
            target.PreferredObjectCreationHandling = options.PreferredObjectCreationHandling;
            target.PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive;
            target.PropertyNamingPolicy = options.PropertyNamingPolicy;
            target.ReadCommentHandling = options.ReadCommentHandling;
            target.ReferenceHandler = options.ReferenceHandler;
            target.RespectNullableAnnotations = options.RespectNullableAnnotations;
            target.RespectRequiredConstructorParameters = options.RespectRequiredConstructorParameters;
            target.UnknownTypeHandling = options.UnknownTypeHandling;
            target.UnmappedMemberHandling = options.UnmappedMemberHandling;
            target.WriteIndented = options.WriteIndented;

            if (options.TypeInfoResolver != null)
            {
                target.TypeInfoResolver = options.TypeInfoResolver;
            }

            foreach (var converter in options.Converters)
            {
                if (target.Converters.Contains(converter))
                    continue;

                if (target.Converters.Any(c => c.Type == converter.Type)) 
                    continue;

                target.Converters.Add(converter);
            }

            return target;
        }

        /// <summary>
        /// Applies the target's serializer options to the current options and returns the result.
        /// </summary>
        /// <param name="target">The target <see cref="JsonSerializerOptions"/> instance from which the current options will be applied. Cannot
        /// be null.</param>
        /// <returns>A <see cref="JsonSerializerOptions"/> instance representing the target options with the current options
        /// applied.</returns>
        public JsonSerializerOptions ApplyFrom(JsonSerializerOptions target)
            => target.ApplyTo(options);
    }
}
