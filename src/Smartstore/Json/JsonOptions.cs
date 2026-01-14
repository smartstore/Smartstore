#nullable enable

using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Smartstore.Json.Polymorphy;
using Smartstore.Utilities;

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
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,

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
            .WithPolymorphyModifier()
            .WithDefaultValueModifier()
            .WithDataContractModifier(),

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

    #region DataContract & polymorphy support modifiers

    /// <summary>
    /// Returns a JSON type info resolver that applies polymorphic serialization and deserialization modifiers to
    /// types and members that opt into polymorphy support by using the <see cref="PolymorphicAttribute"/>.
    /// </summary>
    /// <remarks>Use this method to enable polymorphic type handling in System.Text.Json serialization
    /// like Newtonsoft.Json does with the $type discriminator.</remarks>
    public static IJsonTypeInfoResolver WithPolymorphyModifier(this IJsonTypeInfoResolver typeInfoResolver)
    {
        return Guard.NotNull(typeInfoResolver)
            .WithAddedModifier(PolymorphyModifier.ApplyPolymorphyModifier);
    }

    /// <summary>
    /// Returns a JSON type info resolver that applies DataContract-related modifiers, enabling support for DataContract
    /// attributes during serialization and deserialization.
    /// </summary>
    /// <remarks>The returned resolver will honor <see
    /// cref="System.Runtime.Serialization.IgnoreDataMemberAttribute"/> and ignore the member completely.</remarks>
    public static IJsonTypeInfoResolver WithDataContractModifier(this IJsonTypeInfoResolver typeInfoResolver)
    {
        return Guard.NotNull(typeInfoResolver)
            .WithAddedModifier(ApplyIgnoreDataMemberModifier);
    }

    /// <summary>
    /// Returns a new JSON type info resolver that applies a modifier to handle default values from the [DefaultValue] 
    /// attribute during serialization.
    /// </summary>
    /// <remarks>The returned resolver will honor the <see cref="DefaultValueAttribute"/> annotation, allowing for customized
    /// default values during serialization.</remarks>
    public static IJsonTypeInfoResolver WithDefaultValueModifier(this IJsonTypeInfoResolver typeInfoResolver)
    {
        return Guard.NotNull(typeInfoResolver)
            .WithAddedModifier(ApplyDefaultValueModifier);
    }

    internal static void ApplyIgnoreDataMemberModifier(JsonTypeInfo typeInfo)
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

    internal static void ApplyDefaultValueModifier(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var p in typeInfo.Properties)
        {
            if (p.AttributeProvider?.TryGetAttribute<DefaultValueAttribute>(true, out var attr) ?? false)
            {
                if (typeof(IEnumerable).IsAssignableFrom(p.PropertyType))
                {
                    if (Equals(attr.Value, "[]"))
                    {
                        // Ignore empty lists/arrays/dictionaries when default is "[]"
                        p.ShouldSerialize = (o, value) => !ShouldIgnoreEmptySequence(value as IEnumerable);
                    }
                }
                else
                {
                    var defaultValue = attr.Value.Convert(p.PropertyType);
                    var dv = defaultValue;
                    p.ShouldSerialize = (o, value) => !ShouldIgnoreDefaultValue(value, dv);
                }
            }
        }
    }

    private static bool ShouldIgnoreDefaultValue(object? value, object? defaultValue)
    {
        return Equals(value, defaultValue);
    }

    private static bool ShouldIgnoreEmptySequence(IEnumerable? value)
    {
        return value == null || !value.GetEnumerator().MoveNext();
    }

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
