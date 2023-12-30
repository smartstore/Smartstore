// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Smartstore.ComponentModel
{
    public class FastProperty
    {
        // Using an array rather than IEnumerable, as target will be called on the hot path numerous times.
        private static readonly ConcurrentDictionary<Type, IDictionary<string, FastProperty>> _propertiesCache = new();
        private static readonly ConcurrentDictionary<Type, IDictionary<string, FastProperty>> _visiblePropertiesCache = new();

        // We need to be able to check if a type is a 'ref struct' - but we need to be able to compile
        // for platforms where the attribute is not defined. So we can fetch the attribute
        // by late binding. If the attribute isn't defined, then we assume we won't encounter any
        // 'ref struct' types.
        private static readonly Type IsByRefLikeAttribute = Type.GetType("System.Runtime.CompilerServices.IsByRefLikeAttribute", throwOnError: false)!;

        private bool? _isPublicSettable;
        private bool? _isSequenceType;
        private bool? _isComplexType;

        /// <summary>
        /// Initializes a <see cref="FastProperty"/>.
        /// This constructor does not cache the helper. For caching, use <see cref="GetProperties(object)"/>.
        /// </summary>
        internal FastProperty(PropertyInfo property)
        {
            Property = Guard.NotNull(property);
            Name = property.Name;
        }

        /// <summary>
        /// Gets the backing <see cref="PropertyInfo"/>.
        /// </summary>
        public PropertyInfo Property { get; private set; }

        /// <summary>
        /// Gets (or sets in derived types) the property name.
        /// </summary>
        public virtual string Name { get; protected set; }

        public bool IsPublicSettable
        {
            get
            {
                if (!_isPublicSettable.HasValue)
                {
                    _isPublicSettable = Property.CanWrite && Property.GetSetMethod(false) != null;
                }
                return _isPublicSettable.Value;
            }
        }

        public bool IsComplexType
        {
            get
            {
                if (!_isComplexType.HasValue)
                {
                    var type = Property.PropertyType;
                    _isComplexType = (type.IsClass || type.IsInterface) && !type.IsBasicOrNullableType();
                }
                return _isComplexType.Value;
            }
        }

        public bool IsSequenceType
        {
            get
            {
                if (!_isSequenceType.HasValue)
                {
                    var type = Property.PropertyType;
                    _isSequenceType = type != typeof(string)
                        && (type.IsClosedGenericTypeOf(typeof(IEnumerable<>)) || type.IsClosedGenericTypeOf(typeof(IAsyncEnumerable<>)));
                }
                return _isSequenceType.Value;
            }
        }

        /// <summary>
        /// Returns the property value for the specified <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The object whose property value will be returned.</param>
        /// <returns>The property value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? GetValue(object? instance)
        {
            return Property.GetValue(instance);
        }

        /// <summary>
        /// Sets the property value for the specified <paramref name="instance" />.
        /// </summary>
        /// <param name="instance">The object whose property value will be set.</param>
        /// <param name="value">The property value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(object? instance, object? value)
        {
            Property.SetValue(instance, value);
        }

        /// <summary>
        /// <para>
        /// Creates and caches fast property helpers that expose getters for every non-hidden get property
        /// on the specified type.
        /// </para>
        /// <para>
        /// <see cref="GetVisibleProperties(Type, bool)"/> excludes properties defined on base types that have been
        /// hidden by definitions using the <c>new</c> keyword.
        /// </para>
        /// </summary>
        /// <param name="type">The type to extract property accessors for.</param>
        /// <returns>
        /// A cached array of all public property getters from the type.
        /// </returns>
        public static IReadOnlyDictionary<string, FastProperty> GetVisibleProperties(Type type, bool uncached = false)
        {
            Guard.NotNull(type);

            // Unwrap nullable types. This means Nullable<T>.Value and Nullable<T>.HasValue will not be
            // part of the sequence of properties returned by this method.
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (_visiblePropertiesCache.TryGetValue(type, out var result))
            {
                return (IReadOnlyDictionary<string, FastProperty>)result;
            }

            var visiblePropertiesCache = uncached ? CreateVolatileCache() : _visiblePropertiesCache;

            // The simple and common case, this is normal POCO object - no need to allocate.
            var allPropertiesDefinedOnType = true;
            var allProperties = GetProperties(type, uncached);
            foreach (var prop in allProperties.Values)
            {
                if (prop.Property.DeclaringType != type)
                {
                    allPropertiesDefinedOnType = false;
                    break;
                }
            }

            if (allPropertiesDefinedOnType)
            {
                result = (IDictionary<string, FastProperty>)allProperties;
                visiblePropertiesCache.TryAdd(type, result);
                return allProperties;
            }

            // There's some inherited properties here, so we need to check for hiding via 'new'.
            var filteredProperties = new List<FastProperty>(allProperties.Count);
            foreach (var prop in allProperties.Values)
            {
                var declaringType = prop.Property.DeclaringType;
                if (declaringType == type)
                {
                    filteredProperties.Add(prop);
                    continue;
                }

                // If this property was declared on a base type then look for the definition closest to the
                // the type to see if we should include it.
                var ignoreProperty = false;

                // Walk up the hierarchy until we find the type that actally declares this PropertyInfo.
                var currentTypeInfo = type.GetTypeInfo();
                var declaringTypeInfo = declaringType!.GetTypeInfo();
                while (currentTypeInfo != null && currentTypeInfo != declaringTypeInfo)
                {
                    // We've found a 'more proximal' public definition
                    var declaredProperty = currentTypeInfo.GetDeclaredProperty(prop.Name);
                    if (declaredProperty != null)
                    {
                        ignoreProperty = true;
                        break;
                    }

                    if (currentTypeInfo.BaseType != null)
                    {
                        currentTypeInfo = currentTypeInfo.BaseType.GetTypeInfo();
                    }

                }

                if (!ignoreProperty)
                {
                    filteredProperties.Add(prop);
                }
            }

            result = filteredProperties.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            visiblePropertiesCache.TryAdd(type, result);
            return (IReadOnlyDictionary<string, FastProperty>)result;
        }

        /// <summary>
        /// Creates and caches fast property helpers that expose getters for every public get property on the
        /// specified type.
        /// </summary>
        /// <param name="type">The type to extract property accessors for.</param>
        /// <returns>A cached array of all public property getters from the type of target instance.
        /// </returns>
        public static IReadOnlyDictionary<string, FastProperty> GetProperties(Type type, bool uncached = false)
        {
            Guard.NotNull(type);

            // Unwrap nullable types. This means Nullable<T>.Value and Nullable<T>.HasValue will not be
            // part of the sequence of properties returned by this method.
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (!_propertiesCache.TryGetValue(type, out var props))
            {
                if (uncached)
                {
                    props = Get(type, false);
                }
                else
                {
                    props = _propertiesCache.GetOrAdd(type, t => Get(t, true));
                }
            }

            return (IReadOnlyDictionary<string, FastProperty>)props;

            static IDictionary<string, FastProperty> Get(Type t, bool frozen)
            {
                var candidates = GetCandidateProperties(t);
                var fastProperties = candidates
                    .Select(x => new FastProperty(x))
                    .ToDictionarySafe(x => x.Name, StringComparer.OrdinalIgnoreCase);

                return frozen ? fastProperties.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase) : fastProperties;
            }
        }

        /// <summary>
        /// Extracts all candidate properties that expose getters for every
        /// public get property on the specified type and all its subtypes.
        /// </summary>
        /// <param name="type">The type to extract property accessors for.</param>
        public static IEnumerable<PropertyInfo> GetCandidateProperties(Type type)
        {
            // We avoid loading indexed properties using the Where statement.
            var properties = type.GetRuntimeProperties().Where(IsCandidateProperty);

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsInterface)
            {
                // Reflection does not return information about inherited properties on the interface itself.
                properties = properties.Concat(typeInfo.ImplementedInterfaces.SelectMany(
                    interfaceType => interfaceType.GetRuntimeProperties().Where(IsCandidateProperty)));
            }

            // Polymorphic base properties come last, exclude them to avoid duplicate prop names
            return properties.DistinctBy(p => p.Name);
        }

        private static bool IsCandidateProperty(PropertyInfo property)
        {
            // For improving application startup time, do not use GetIndexParameters() api early in this check as it
            // creates a copy of parameter array and also we would like to check for the presence of a get method
            // and short circuit asap.
            return property.GetIndexParameters().Length == 0 &&
                property.GetMethod != null &&
                property.GetMethod.IsPublic &&
                !property.GetMethod.IsStatic &&
                // FastProperty can't work with ref structs.
                !IsRefStructProperty(property) &&
                // Indexed properties are not useful (or valid) for grabbing properties off an object.
                property.GetMethod.GetParameters().Length == 0;
        }

        // FastProperty can't really interact with ref-struct properties since they can't be 
        // boxed and can't be used as generic types. We just ignore them.
        //
        // see: https://github.com/aspnet/Mvc/issues/8545
        private static bool IsRefStructProperty(PropertyInfo property)
        {
            return
                IsByRefLikeAttribute != null &&
                property.PropertyType.IsValueType &&
                property.PropertyType.IsDefined(IsByRefLikeAttribute);
        }

        private static ConcurrentDictionary<Type, IDictionary<string, FastProperty>> CreateVolatileCache()
        {
            return new ConcurrentDictionary<Type, IDictionary<string, FastProperty>>();
        }

        class PropertyKey : Tuple<Type, string>
        {
            public PropertyKey(Type type, string propertyName)
                : base(type, propertyName)
            {
            }
            public Type Type { get { return Item1; } }
            public string PropertyName { get { return Item2; } }
        }
    }
}