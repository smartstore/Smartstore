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

            return
                type.IsArray ||
                typeof(IEnumerable).IsAssignableFrom(type) ||
                type == typeof(Array) ||
                type.IsClosedGenericTypeOf(typeof(IAsyncEnumerable<>));
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
            else if (
                type.TryGetClosedGenericTypeOf(typeof(IEnumerable<>), out var closedType) ||
                type.TryGetClosedGenericTypeOf(typeof(IAsyncEnumerable<>), out closedType))
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

            if (
                type.TryGetClosedGenericTypeOf(typeof(IEnumerable<>), out var closedType) ||
                type.TryGetClosedGenericTypeOf(typeof(IAsyncEnumerable<>), out closedType))
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
            var attributes = target.GetCustomAttributes(typeof(TAttribute), inherits);
            if (attributes.Length > 1)
            {
                throw Error.MoreThanOneElement();
            }

            return attributes.Length == 0 ? null : (TAttribute)attributes[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAttribute<TAttribute>(bool inherits) where TAttribute : Attribute
        {
            return target.IsDefined(typeof(TAttribute), inherits);
        }

        public IEnumerable<TAttribute> GetAttributes<TAttribute>(bool inherits) where TAttribute : Attribute
        {
            var attributes = (IEnumerable<TAttribute>)target.GetCustomAttributes(typeof(TAttribute), inherits);

            if (typeof(IOrdered).IsAssignableFrom(typeof(TAttribute)))
            {
                return attributes
                    .Cast<IOrdered>()
                    .OrderBy(x => x.Ordinal)
                    .Cast<TAttribute>();
            }
            else
            {
                return attributes;
            }
        }
    }

    extension(MemberInfo member)
    {
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
}