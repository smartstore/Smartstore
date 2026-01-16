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
        public string? AssemblyQualifiedNameWithoutVersion()
        {
            return type.AssemblyQualifiedName != null
                ? type.FullName + ", " + type.Assembly.GetName().Name
                : null;
        }

        public bool HasDefaultConstructor()
        {
            return type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null;
        }

        public bool IsAny(params Type[] checkTypes)
        {
            return checkTypes.Any(possibleType => possibleType == type);
        }

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

        public bool IsBasicOrNullableType()
        {
            return type.IsBasicType() || Nullable.GetUnderlyingType(type) != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullableType()
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullableType(out Type underlyingType)
        {
            underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            return underlyingType != type;
        }

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

        public bool IsPlainObjectType()
        {
            return type.IsClass && !type.IsSequenceType() && !type.IsBasicOrNullableType();
        }

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

        public bool IsCollectionType([NotNullWhen(true)] out Type? elementType)
        {
            elementType = null;

            if (type.IsBasicOrNullableType())
            {
                return false;
            }

            if (type.TryGetClosedGenericTypeOf(typeof(ICollection<>), out var closedType))
            {
                elementType = closedType.GetGenericArguments()[0];
            }

            return elementType != null;
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDictionaryType()
        {
            return typeof(IDictionary).IsAssignableFrom(type) || 
                type.TryGetClosedGenericTypeOf(typeof(IDictionary<,>), out _) ||
                type.TryGetClosedGenericTypeOf(typeof(IReadOnlyDictionary<,>), out _);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Type GetNonNullableType()
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOpenGeneric()
        {
            return type.IsGenericTypeDefinition || type.ContainsGenericParameters;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsClosedGenericTypeOf(Type openGeneric)
        {
            return type.TryGetClosedGenericTypeOf(openGeneric, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsClosedGenericTypeOf(Type openGeneric, [NotNullWhen(true)] out Type? closedGeneric)
        {
            return type.TryGetClosedGenericTypeOf(openGeneric, out closedGeneric);
        }

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