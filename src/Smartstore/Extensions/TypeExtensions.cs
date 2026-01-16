#nullable enable

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.Hosting;
using Smartstore.Domain;

namespace Smartstore;

public static class TypeExtensions
{
    extension(Type type)
    {
        /// <summary>
        /// Returns the assembly-qualified name of the type without including version, culture, or public key token
        /// information.
        /// </summary>
        /// <remarks>For generic types, the returned string includes the generic type definition and
        /// recursively omits version, culture, and public key token information from all generic type arguments. This
        /// format is useful for scenarios where type identity is required without binding to a specific assembly
        /// version.</remarks>
        /// <returns>A string containing the assembly-qualified name of the type, omitting version, culture, and public key token
        /// details; or null if the type information is unavailable.</returns>
        public string? AssemblyQualifiedNameWithoutVersion()
        {
            // Get the assembly name without version, culture, and public key token
            var assemblyName = type.Assembly.GetName().Name!;

            if (type.IsGenericType)
            {
                // Get the generic type definition (e.g., System.Collections.Generic.List`1)
                var genericDefinition = type.GetGenericTypeDefinition().FullName!;

                // Recursively clean all generic type arguments
                var genericArguments = type.GetGenericArguments()
                    .Select(t => $"[{t.AssemblyQualifiedNameWithoutVersion()}]");

                var args = string.Join(",", genericArguments);

                // Construct the cleaned generic string format
                return $"{genericDefinition}[{args}], {assemblyName}";
            }

            // Return the simple name for non-generic types
            return $"{type.FullName}, {assemblyName}";
        }

        public bool HasDefaultConstructor()
        {
            return type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null;
        }

        public bool IsAny(params Type[] checkTypes)
        {
            return checkTypes.Any(possibleType => possibleType == type);
        }

        /// <summary>
        /// Determines whether the current type is compatible with the specified target type for assignment or
        /// conversion.
        /// </summary>
        /// <remarks>This method checks for compatibility based on standard .NET assignment rules,
        /// including reference type assignability and implicit numeric conversions for value types. For nullable types,
        /// compatibility is determined using their underlying non-nullable types.</remarks>
        public bool IsCompatibleWith(Type target)
        {
            if (type == target)
                return true;

            if (!target.IsValueType)
                return target.IsAssignableFrom(type);

            var nonNullableType = type.GetNonNullableType();
            var targetNonNullableType = target.GetNonNullableType();

            if ((nonNullableType == type) || (targetNonNullableType != target))
            {
                var code = nonNullableType.IsEnum ? TypeCode.Object : Type.GetTypeCode(nonNullableType);
                var targetCode = targetNonNullableType.IsEnum ? TypeCode.Object : Type.GetTypeCode(targetNonNullableType);

                switch (code)
                {
                    case TypeCode.SByte:
                        switch (targetCode)
                        {
                            case TypeCode.SByte:
                            case TypeCode.Int16:
                            case TypeCode.Int32:
                            case TypeCode.Int64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;
                    case TypeCode.Byte:
                        switch (targetCode)
                        {
                            case TypeCode.Byte:
                            case TypeCode.Int16:
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;
                    case TypeCode.Int16:
                        switch (targetCode)
                        {
                            case TypeCode.Int16:
                            case TypeCode.Int32:
                            case TypeCode.Int64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;
                    case TypeCode.UInt16:
                        switch (targetCode)
                        {
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;
                    case TypeCode.Int32:
                        switch (targetCode)
                        {
                            case TypeCode.Int32:
                            case TypeCode.Int64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;
                    case TypeCode.UInt32:
                        switch (targetCode)
                        {
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;
                    case TypeCode.Int64:
                        switch (targetCode)
                        {
                            case TypeCode.Int64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;
                    case TypeCode.UInt64:
                        switch (targetCode)
                        {
                            case TypeCode.UInt64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;
                    case TypeCode.Single:
                        switch (targetCode)
                        {
                            case TypeCode.Single:
                            case TypeCode.Double:
                                return true;
                        }
                        break;
                    default:
                        if (nonNullableType == targetNonNullableType)
                        {
                            return true;
                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a sequence of types that the current type implements or derives from, including all implemented
        /// interfaces and base types up to but not including System.Object.
        /// </summary>
        /// <remarks>The returned sequence includes all interfaces implemented by the type, followed by
        /// the type itself and its base types, in order from most derived to least derived. System.Object is not
        /// included in the result.</remarks>
        public IEnumerable<Type> GetTypesAssignableFrom()
        {
            var interfaces = type.GetInterfaces();

            for (var i = 0; i < interfaces.Length; i++)
            {
                yield return interfaces[i];
            }

            var current = type;
            while (current != null && current != typeof(object))
            {
                yield return current;
                current = current.BaseType;
            }
        }

        /// <summary>
        /// Determines whether the current type is considered a basic .NET type, such as a primitive, string, or common
        /// value type.
        /// </summary>
        /// <remarks>Basic types are commonly used for serialization, data transfer, and value
        /// representation. This method can be used to identify types that are typically handled as simple values rather
        /// than complex objects.</remarks>
        /// <returns>true if the type is a primitive, enumeration, string, decimal, DateTime, DateTimeOffset, DateOnly, TimeOnly,
        /// TimeSpan, Guid, or byte array; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsBasicType()
        {
            return
                type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(DateOnly) ||
                type == typeof(TimeOnly) ||
                type == typeof(TimeSpan) ||
                type == typeof(Guid) ||
                type == typeof(byte[]);
        }

        /// <summary>
        /// Determines whether the current type is a basic type or a nullable basic type.
        /// </summary>
        public bool IsBasicOrNullableType()
        {
            return type.IsBasicType() || Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// Determines whether the current type is a nullable value type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullableType()
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// Determines whether the current type is a nullable value type and retrieves its underlying type.
        /// </summary>
        /// <param name="underlyingType">When this method returns, contains the underlying type if the current type is a nullable value type;
        /// otherwise, contains the current type. This parameter is passed uninitialized.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullableType(out Type underlyingType)
        {
            underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            return underlyingType != type;
        }

        /// <summary>
        /// Determines whether the current type represents a numeric value, including integer and floating-point types.
        /// </summary>
        /// <remarks>This method considers both standard numeric types and their nullable counterparts as
        /// numeric. Types such as enums, booleans, and non-numeric objects are not considered numeric.</remarks>
        /// <returns>true if the type is a numeric type such as an integer, decimal, double, or single, or a nullable numeric
        /// type; otherwise, false.</returns>
        public bool IsNumericType()
        {
            if (type.IsIntegerType())
            {
                return true;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                case TypeCode.Object:
                    return type.IsNullableType(out var underlyingType) && underlyingType.IsNumericType();
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the underlying type is a built-in integer type.
        /// </summary>
        /// <remarks>This method considers the following types as integer types: SByte, Byte, Int16,
        /// UInt16, Int32, UInt32, Int64, and UInt64. Other numeric types, such as Decimal, Single, Double, and
        /// BigInteger, are not considered integer types by this method.</remarks>
        public bool IsIntegerType()
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsEnumType()
        {
            return type.GetNonNullableType().IsEnum;
        }

        public bool IsStructType()
        {
            return type.IsValueType && !type.IsBasicType();
        }

        /// <summary>
        /// Determines whether the current type represents a plain object type, excluding sequences and basic or
        /// nullable types.
        /// </summary>
        public bool IsPlainObjectType()
        {
            return type.IsClass && !type.IsSequenceType() && !type.IsBasicOrNullableType();
        }

        /// <summary>
        /// Determines whether the represented type is marked with the <see cref="CompilerGeneratedAttribute"/>.
        /// </summary>
        /// <remarks>This method can be used to identify types that are generated by the compiler, such as
        /// anonymous types, iterator state machines, or closure classes. Compiler-generated types are typically not
        /// intended for direct use in application code.</remarks>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCompilerGenerated()
        {
            return type.IsDefined(typeof(CompilerGeneratedAttribute), false);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRazorCompiledItem()
        {
            return type.IsDefined(typeof(RazorCompiledItemAttribute), false);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDelegate()
        {
            return type.IsSubclassOf(typeof(Delegate));
        }

        [DebuggerStepThrough]
        public bool IsAnonymousType()
        {
            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (definition.IsClass && definition.IsSealed && definition.Attributes.HasFlag(TypeAttributes.NotPublic))
                {
                    var attributes = definition.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false);
                    if (attributes?.Length > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the current type represents a sequence type, such as an array or a type that implements
        /// <see cref="IEnumerable"/>.
        /// </summary>
        /// <remarks>A sequence type is any type that can be enumerated, including arrays and types that
        /// implement <see cref="IEnumerable"/>. Basic types and nullable types are not considered sequence
        /// types.</remarks>
        /// <returns>true if the type is an array, implements <see cref="IEnumerable"/>, or is <see cref="Array"/>; otherwise,
        /// false.</returns>
        public bool IsSequenceType()
        {
            if (type.IsBasicOrNullableType())
            {
                return false;
            }

            return (
                type.IsArray ||
                typeof(IEnumerable).IsAssignableFrom(type) ||
                type == typeof(Array));
        }

        /// <inheritdoc cref="IsSequenceType(Type)" />
        /// <param name="elementType">When this method returns <see langword="true"/>, contains the type of the elements in the sequence;
        /// otherwise, <see langword="null"/>.</param>
        public bool IsSequenceType([NotNullWhen(true)] out Type? elementType)
        {
            elementType = null;

            if (type.IsBasicOrNullableType())
            {
                return false;
            }

            if (type.IsArray)
            {
                elementType = type.GetElementType();
            }
            else if (type.TryGetClosedGenericTypeOf(typeof(IEnumerable<>), out var closedType))
            {
                elementType = closedType.GetGenericArguments()[0];
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type) || type == typeof(Array))
            {
                elementType = typeof(object);
            }

            return elementType != null;
        }

        /// <summary>
        /// Determines whether the current type implements a closed generic IEnumerable<T> interface and retrieves the
        /// element type if it does.
        /// </summary>
        /// <remarks>This method excludes basic and nullable types from being considered enumerable. If
        /// the type implements multiple IEnumerable<T> interfaces, the first matching generic argument is
        /// returned.</remarks>
        /// <param name="elementType">When this method returns <see langword="true"/>, contains the element type of the IEnumerable; otherwise,
        /// <see langword="null"/>.</param>
        public bool IsEnumerableType([NotNullWhen(true)] out Type? elementType)
        {
            elementType = null;

            if (type.IsBasicOrNullableType())
            {
                return false;
            }

            if (type.TryGetClosedGenericTypeOf(typeof(IEnumerable<>), out var closedType))
            {
                elementType = closedType.GetGenericArguments()[0];
            }

            return elementType != null;
        }

        /// <summary>
        /// Determines whether the current type implements the IAsyncEnumerable<T> interface and retrieves the element
        /// type if it does.
        /// </summary>
        public bool IsAsyncEnumerableType([NotNullWhen(true)] out Type? elementType)
        {
            elementType = null;

            if (type.IsBasicOrNullableType())
            {
                return false;
            }

            if (type.TryGetClosedGenericTypeOf(typeof(IAsyncEnumerable<>), out var closedType))
            {
                elementType = closedType.GetGenericArguments()[0];
            }

            return elementType != null;
        }

        /// <summary>
        /// Determines whether the current type represents a collection type and retrieves its element type if
        /// applicable.
        /// </summary>
        /// <remarks>This method does not consider basic or nullable types as collections. If the type is
        /// a closed generic implementation of <see cref="ICollection{T}"/> or <see cref="IReadOnlyCollection{T}"/>, the
        /// element type is provided via the <paramref name="elementType"/> parameter.</remarks>
        public bool IsCollectionType([NotNullWhen(true)] out Type? elementType)
        {
            elementType = null;

            if (type.IsBasicOrNullableType())
            {
                return false;
            }

            if (
                type.TryGetClosedGenericTypeOf(typeof(ICollection<>), out var closedType) ||
                type.TryGetClosedGenericTypeOf(typeof(IReadOnlyCollection<>), out closedType))
            {
                elementType = closedType.GetGenericArguments()[0];
            }

            return elementType != null;
        }

        /// <summary>
        /// Determines whether the current type represents a set type and retrieves its element type if applicable.
        /// </summary>
        /// <remarks>This method considers both <see cref="ISet{T}"/> and <see cref="IReadOnlySet{T}"/> as
        /// valid set types. Basic types and nullable types are not considered set types.</remarks>
        public bool IsSetType([NotNullWhen(true)] out Type? elementType)
        {
            elementType = null;

            if (type.IsBasicOrNullableType())
            {
                return false;
            }

            if (
                type.TryGetClosedGenericTypeOf(typeof(ISet<>), out var closedType) ||
                type.TryGetClosedGenericTypeOf(typeof(IReadOnlySet<>), out closedType))
            {
                elementType = closedType.GetGenericArguments()[0];
            }

            return elementType != null;
        }

        /// <inheritdoc cref="IsDictionaryType(Type, out Type?, out Type?)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDictionaryType()
        {
            return typeof(IDictionary).IsAssignableFrom(type) || 
                type.TryGetClosedGenericTypeOf(typeof(IDictionary<,>), out _) ||
                type.TryGetClosedGenericTypeOf(typeof(IReadOnlyDictionary<,>), out _);
        }

        /// <summary>
        /// Determines whether the current type is a dictionary type and retrieves its key and value types if
        /// applicable.
        /// </summary>
        /// <remarks>This method supports both mutable and read-only generic dictionary interfaces. If the
        /// type implements either interface, the corresponding generic type arguments are provided in <paramref
        /// name="keyType"/> and <paramref name="valueType"/>.</remarks>
        public bool IsDictionaryType([NotNullWhen(true)] out Type? keyType, [NotNullWhen(true)] out Type? valueType)
        {
            keyType = null;
            valueType = null;

            if (
                type.TryGetClosedGenericTypeOf(typeof(IDictionary<,>), out var closedType) || 
                type.TryGetClosedGenericTypeOf(typeof(IReadOnlyDictionary<,>), out closedType))
            {
                var args = closedType.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the underlying non-nullable type if the current type is a nullable value type; otherwise, returns
        /// the current type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Type GetNonNullableType()
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        /// <summary>
        /// Determines whether the current type represents an open generic type.
        /// </summary>
        /// <returns>true if the type is a generic type definition or contains unassigned generic parameters; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOpenGeneric()
        {
            return type.IsGenericTypeDefinition || type.ContainsGenericParameters;
        }

        /// <inheritdoc cref="IsClosedGenericTypeOf(Type, Type, out Type?)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsClosedGenericTypeOf(Type openGeneric)
        {
            return type.TryGetClosedGenericTypeOf(openGeneric, out _);
        }

        /// <summary>
        /// Determines whether the current type is a closed constructed type of the specified open generic type.
        /// </summary>
        /// <param name="openGeneric">The open generic type definition to compare with. Must be a generic type definition; for example,
        /// typeof(List<>).</param>
        /// <param name="closedGeneric">When this method returns, contains the closed constructed type if the current type is a closed generic type
        /// of the specified open generic; otherwise, null. This parameter is passed uninitialized.</param>
        /// <returns>true if the current type is a closed constructed type of the specified open generic type; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsClosedGenericTypeOf(Type openGeneric, [NotNullWhen(true)] out Type? closedGeneric)
        {
            return type.TryGetClosedGenericTypeOf(openGeneric, out closedGeneric);
        }

        /// <summary>
        /// Returns all closed generic types that are constructed from the specified open generic type.
        /// </summary>
        /// <param name="openGeneric">The open generic type definition to search for. Must be an open generic type; otherwise, an empty collection
        /// is returned.</param>
        public IEnumerable<Type> GetClosedGenericTypesOf(Type openGeneric)
        {
            if (!openGeneric.IsOpenGeneric())
            {
                return [];
            }

            return type.GetClosedGenericTypesOfCore(openGeneric);
        }

        private IEnumerable<Type> GetClosedGenericTypesOfCore(Type openGeneric)
        {
            foreach (var t in type.GetTypesAssignableFrom())
            {
                if (!t.ContainsGenericParameters && t.IsGenericType && t.GetGenericTypeDefinition() == openGeneric)
                {
                    yield return t;
                }
            }
        }

        private bool TryGetClosedGenericTypeOf(Type openGeneric, [NotNullWhen(true)] out Type? closedGeneric)
        {
            closedGeneric = null;

            if (!openGeneric.IsOpenGeneric())
            {
                return false;
            }

            foreach (var t in type.GetTypesAssignableFrom())
            {
                if (!t.ContainsGenericParameters && t.IsGenericType && t.GetGenericTypeDefinition() == openGeneric)
                {
                    closedGeneric = t;
                    return true;
                }
            }

            return false;
        }
    }

    extension(MethodBase method)
    {
        public MethodInvoker CreateInvoker()
            => MethodInvoker.Create(method);

        public PropertyInfo? GetPropertyFromMethod()
        {
            Guard.NotNull(method);

            PropertyInfo? property = null;

            if (method.IsSpecialName)
            {
                var containingType = method.DeclaringType;
                if (containingType != null)
                {
                    if (method.Name.StartsWith("get_", StringComparison.Ordinal) ||
                        method.Name.StartsWith("set_", StringComparison.Ordinal))
                    {
                        var propertyName = method.Name[4..];
                        property = containingType.GetProperty(propertyName);
                    }
                }
            }

            return property;
        }
    }

    extension(ICustomAttributeProvider target)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetAttribute<TAttribute>(bool inherits, [NotNullWhen(true)] out TAttribute? attribute) where TAttribute : Attribute
        {
            attribute = target.GetAttribute<TAttribute>(inherits);
            return attribute != null;
        }

        /// <summary>
        /// Retrieves a custom attribute of type <typeparamref name="TAttribute"/> that is applied to the target
        /// element, optionally searching the inheritance chain.
        /// </summary>
        /// <remarks>If multiple attributes of the specified type are applied to the target element, an
        /// exception is thrown. If no such attribute is found, the method returns null. When <paramref
        /// name="inherits"/> is true, the method searches base classes or interfaces as appropriate for inherited
        /// attributes.</remarks>
        /// <param name="inherits">true to search the inheritance chain for the attribute; otherwise, false.</param>
        /// <exception cref="InvalidOperationException">Thrown if more than one attribute of type <typeparamref name="TAttribute"/> is found on the target element.</exception>
        public TAttribute? GetAttribute<TAttribute>(bool inherits) where TAttribute : Attribute
        {
            // 1. Fast Path: Direct check (IsDefined is cheaper than GetCustomAttributes)
            var attrs = target.GetCustomAttributes(typeof(TAttribute), inherits);

            if (attrs.Length == 0 && inherits && target is MemberInfo mi)
            {
                // 2. Slow Path: Manual hierarchy crawl for overrides/shadows
                return GetAttributeFromHierarchy<TAttribute>(mi);
            }

            if (attrs.Length == 0) return null;
            if (attrs.Length > 1) throw new InvalidOperationException("More than one attribute found.");

            return (TAttribute)attrs[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAttribute<TAttribute>(bool inherits) where TAttribute : Attribute
        {
            // Best performance: IsDefined uses internal CLR metadata pointers
            if (target.IsDefined(typeof(TAttribute), inherits)) return true;

            if (inherits && target is MemberInfo mi)
            {
                // Fallback for shadowed/overridden members
                return GetAttributeFromHierarchy<TAttribute>(mi) != null;
            }

            return false;
        }

        public IEnumerable<TAttribute> GetAttributes<TAttribute>(bool inherits) where TAttribute : Attribute
        {
            var attrs = target.GetCustomAttributes(typeof(TAttribute), inherits);

            // If nothing found but inheritance requested, check base members
            if (attrs.Length == 0 && inherits && target is MemberInfo mi)
            {
                var fromBase = GetAttributeFromHierarchy<TAttribute>(mi);
                if (fromBase != null) return [fromBase];
                return [];
            }

            if (attrs.Length == 0) return [];

            // Optimized sort for IOrdered
            if (typeof(IOrdered).IsAssignableFrom(typeof(TAttribute)))
            {
                var list = new List<TAttribute>(attrs.Length);
                for (int i = 0; i < attrs.Length; i++) list.Add((TAttribute)attrs[i]);
                list.Sort((x, y) => ((IOrdered)x!).Ordinal.CompareTo(((IOrdered)y!).Ordinal));
                return list;
            }

            // Avoid IEnumerable overhead by returning a typed array
            var result = new TAttribute[attrs.Length];
            Array.Copy(attrs, result, attrs.Length);
            return result;
        }
    }

    extension(MemberInfo member)
    {
        /// <summary>
        /// Determines if the member overrides a definition from a base class.
        /// </summary>
        /// <returns>True if the member is an override; otherwise, false.</returns>
        public bool IsOverride()
        {
            if (member is PropertyInfo pi)
            {
                // Properties are overridden via their accessor methods (get/set).
                var accessor = pi.GetMethod ?? pi.SetMethod;
                if (accessor == null) return false;

                // GetBaseDefinition returns the method where the implementation was first declared.
                // If the declaring type differs from the base definition's type, it's an override.
                return accessor.GetBaseDefinition().DeclaringType != accessor.DeclaringType;
            }

            if (member is MethodInfo method)
            {
                return method.GetBaseDefinition().DeclaringType != method.DeclaringType;
            }

            // Fields, Events, or Types cannot be "overridden" in the classical IL sense.
            return false;
        }

        public TAttribute[] GetAllAttributes<TAttribute>(bool inherits)
            where TAttribute : Attribute
        {
            List<TAttribute> attributes = [];

            if (member.DeclaringType != null)
            {
                attributes.AddRange(member.DeclaringType.GetCustomAttributes<TAttribute>(inherits));

                if (member is MethodBase methodBase)
                {
                    var prop = methodBase.GetPropertyFromMethod();
                    if (prop != null)
                    {
                        attributes.AddRange(prop.GetCustomAttributes<TAttribute>(inherits));
                    }
                }
            }

            attributes.AddRange(member.GetCustomAttributes<TAttribute>(inherits));
            return attributes.ToArray();
        }
    }

    /// <summary>
    /// Crawls up the inheritance chain to find attributes on overridden or shadowed members.
    /// This is only called if the standard provider fails.
    /// </summary>
    private static TAttribute? GetAttributeFromHierarchy<TAttribute>(MemberInfo mi) where TAttribute : Attribute
    {
        // GUARD: Check if the member is actually an override.
        // If the base definition is declared in the same type, it's NOT an override.
        if (!mi.IsOverride())
        {
            return null;
        }

        // Only proceed to expensive hierarchy crawl if it's an actual override.
        var currentType = mi.DeclaringType?.BaseType;
        var memberName = mi.Name;
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        while (currentType != null && currentType != typeof(object))
        {
            MemberInfo? baseMember = (mi is PropertyInfo)
                ? currentType.GetProperty(memberName, flags)
                : (MemberInfo?)currentType.GetMethod(memberName, flags);

            if (baseMember != null)
            {
                // We check with inherits:false because we are already iterating through the types.
                var attr = baseMember.GetCustomAttributes(typeof(TAttribute), false);
                if (attr.Length > 0) return (TAttribute)attr[0];

                // Optimization: Stop if we reached the root definition of the override chain.
                if (baseMember is MethodInfo mb && mb.GetBaseDefinition() == mb) break;
                if (baseMember is PropertyInfo pb)
                {
                    var acc = pb.GetMethod ?? pb.SetMethod;
                    if (acc != null && acc.GetBaseDefinition() == acc) break;
                }
            }
            currentType = currentType.BaseType;
        }
        return null;
    }
}