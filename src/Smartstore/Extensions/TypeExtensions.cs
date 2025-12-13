using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.Hosting;
using Smartstore.Domain;

namespace Smartstore;

public static class TypeExtensions
{
    extension(Type type)
    {
        public string AssemblyQualifiedNameWithoutVersion()
        {
            return type.AssemblyQualifiedName != null
                ? type.FullName + ", " + type.Assembly.GetName().Name
                : null;
        }

        public bool HasDefaultConstructor()
        {
            return type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null;
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
                type == typeof(Array);
        }

        public bool IsSequenceType(out Type elementType)
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
            else if (type.IsClosedGenericTypeOf(typeof(IEnumerable<>), out var closedType))
            {
                elementType = closedType.GetGenericArguments()[0];
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type) || type == typeof(Array))
            {
                elementType = typeof(object);
            }

            return elementType != null;
        }

        public bool IsEnumerableType(out Type elementType)
        {
            elementType = null;

            if (type.IsBasicOrNullableType())
            {
                return false;
            }

            if (type.IsClosedGenericTypeOf(typeof(IEnumerable<>), out var closedType))
            {
                elementType = closedType.GetGenericArguments()[0];
            }

            return elementType != null;
        }

        public bool IsCollectionType(out Type elementType)
        {
            elementType = null;

            if (type.IsBasicOrNullableType())
            {
                return false;
            }

            if (type.IsClosedGenericTypeOf(typeof(ICollection<>), out var closedType))
            {
                elementType = closedType.GetGenericArguments()[0];
            }

            return elementType != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDictionaryType()
        {
            return type.IsClosedGenericTypeOf(typeof(IDictionary<,>));
        }

        public bool IsDictionaryType(out Type keyType, out Type valueType)
        {
            keyType = null;
            valueType = null;

            if (type.IsClosedGenericTypeOf(typeof(IDictionary<,>), out var closedType))
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
            return type.GetClosedGenericTypesOf(openGeneric).Any();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsClosedGenericTypeOf(Type openGeneric, out Type closedGeneric)
        {
            closedGeneric = type.GetClosedGenericTypesOf(openGeneric).FirstOrDefault();
            return closedGeneric != null;
        }

        public IEnumerable<Type> GetClosedGenericTypesOf(Type openGeneric)
        {
            if (!openGeneric.IsOpenGeneric())
            {
                return Enumerable.Empty<Type>();
            }

            return type.GetTypesAssignableFrom()
                .Where(t => !t.ContainsGenericParameters && t.IsGenericType && t.GetGenericTypeDefinition() == openGeneric);
        }
    }

    extension(MethodBase method)
    {
        public MethodInvoker CreateInvoker()
            => MethodInvoker.Create(method);

        public PropertyInfo GetPropertyFromMethod()
        {
            Guard.NotNull(method);

            PropertyInfo property = null;

            if (method.IsSpecialName)
            {
                var containingType = method.DeclaringType;
                if (containingType != null)
                {
                    if (method.Name.StartsWith("get_", StringComparison.InvariantCulture) ||
                        method.Name.StartsWith("set_", StringComparison.InvariantCulture))
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
        public bool TryGetAttribute<TAttribute>(bool inherits, out TAttribute attribute) where TAttribute : Attribute
        {
            attribute = target.GetAttribute<TAttribute>(inherits);
            return attribute != null;
        }

        public TAttribute GetAttribute<TAttribute>(bool inherits) where TAttribute : Attribute
        {
            if (target.IsDefined(typeof(TAttribute), inherits))
            {
                var attributes = target.GetCustomAttributes(typeof(TAttribute), inherits);
                if (attributes.Length > 1)
                {
                    throw Error.MoreThanOneElement();
                }

                return (TAttribute)attributes[0];
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAttribute<TAttribute>(bool inherits) where TAttribute : Attribute
        {
            return target.IsDefined(typeof(TAttribute), inherits);
        }

        public TAttribute[] GetAttributes<TAttribute>(bool inherits) where TAttribute : Attribute
        {
            if (target.IsDefined(typeof(TAttribute), inherits))
            {
                var attributes = target
                    .GetCustomAttributes(typeof(TAttribute), inherits)
                    .Cast<TAttribute>();

                return SortAttributesIfPossible(attributes).ToArray();
            }

            return Array.Empty<TAttribute>();
        }
    }

    extension(MemberInfo member)
    {
        public TAttribute[] GetAllAttributes<TAttribute>(bool inherits)
            where TAttribute : Attribute
        {
            List<TAttribute> attributes = new();

            if (member.DeclaringType != null)
            {
                attributes.AddRange(member.DeclaringType.GetAttributes<TAttribute>(inherits));

                if (member is MethodBase methodBase)
                {
                    var prop = methodBase.GetPropertyFromMethod();
                    if (prop != null)
                    {
                        attributes.AddRange(prop.GetAttributes<TAttribute>(inherits));
                    }
                }
            }

            attributes.AddRange(member.GetAttributes<TAttribute>(inherits));
            return attributes.ToArray();
        }
    }

    internal static IEnumerable<TAttribute> SortAttributesIfPossible<TAttribute>(IEnumerable<TAttribute> attributes)
        where TAttribute : Attribute
    {
        if (typeof(IOrdered).IsAssignableFrom(typeof(TAttribute)))
        {
            return attributes
                .Cast<IOrdered>()
                .OrderBy(x => x.Ordinal)
                .Cast<TAttribute>();
        }

        return attributes;
    }
}
