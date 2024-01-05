using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.Hosting;
using Smartstore.Domain;

namespace Smartstore
{
    public static class TypeExtensions
    {
        #region Common

        public static string AssemblyQualifiedNameWithoutVersion(this Type type)
        {
            return type.AssemblyQualifiedName != null
                ? type.FullName + ", " + type.Assembly.GetName().Name
                : null;
        }

        /// <summary>
        /// Creates a <see cref="MethodInvoker"/> instance for the given <paramref name="method"/>>.
        /// </summary>
        public static MethodInvoker CreateInvoker(this MethodBase method)
            => MethodInvoker.Create(method);

        /// <summary>
        /// Given a MethodBase for a property's get or set method,
        /// return the corresponding property info.
        /// </summary>
        /// <param name="method">MethodBase for the property's get or set method.</param>
        /// <returns>PropertyInfo for the property, or null if method is not part of a property.</returns>
        public static PropertyInfo GetPropertyFromMethod(this MethodBase method)
        {
            Guard.NotNull(method);
            
            PropertyInfo property = null;

            if (method.IsSpecialName)
            {
                Type containingType = method.DeclaringType;
                if (containingType != null)
                {
                    if (method.Name.StartsWith("get_", StringComparison.InvariantCulture) ||
                        method.Name.StartsWith("set_", StringComparison.InvariantCulture))
                    {
                        string propertyName = method.Name[4..];
                        property = containingType.GetProperty(propertyName);
                    }
                }
            }

            return property;
        }

        public static bool HasDefaultConstructor(this Type type)
        {
            return type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null;
        }

        public static bool IsCompatibleWith(this Type source, Type target)
        {
            if (source == target)
                return true;

            if (!target.IsValueType)
                return target.IsAssignableFrom(source);

            var nonNullableType = source.GetNonNullableType();
            var type = target.GetNonNullableType();

            if ((nonNullableType == source) || (type != target))
            {
                var code = nonNullableType.IsEnum ? TypeCode.Object : Type.GetTypeCode(nonNullableType);
                var code2 = type.IsEnum ? TypeCode.Object : Type.GetTypeCode(type);

                switch (code)
                {
                    case TypeCode.SByte:
                        switch (code2)
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
                        switch (code2)
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
                        switch (code2)
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
                        switch (code2)
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
                        switch (code2)
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
                        switch (code2)
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
                        switch (code2)
                        {
                            case TypeCode.Int64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;
                    case TypeCode.UInt64:
                        switch (code2)
                        {
                            case TypeCode.UInt64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                return true;
                        }
                        break;
                    case TypeCode.Single:
                        switch (code2)
                        {
                            case TypeCode.Single:
                            case TypeCode.Double:
                                return true;
                        }
                        break;
                    default:
                        if (nonNullableType == type)
                        {
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets all types that are assignable from <paramref name="source"/>, except for <see cref="typeof(object)"/>
        /// </summary>
        /// <param name="source">The type to get assignable types for.</param>
        /// <returns>
        /// All interface types in the hierarchy chain, 
        /// all base types (except <see cref="typeof(object)"/>) and the source type itself.
        /// </returns>
        public static IEnumerable<Type> GetTypesAssignableFrom(this Type source)
        {
            var interfaces = source.GetInterfaces();

            for (var i = 0; i < interfaces.Length; i++)
            {
                yield return interfaces[i];
            }

            while (source != null && source != typeof(object))
            {
                yield return source;
                source = source.BaseType;
            }
        }

        #endregion

        #region Is...Type

        /// <summary>
        /// Checks whether given type is primitive, enum, <see cref="typeof(string)"/>, 
        /// <see cref="typeof(decimal)"/>, <see cref="typeof(DateTime)"/>, 
        /// <see cref="typeof(TimeSpan)"/>, <see cref="typeof(Guid)"/>, 
        /// <see cref="typeof(byte[])"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBasicType(this Type type)
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

        /// <summary>
        /// Checks whether given type is a predefined basic type or a nullable type. 
        /// </summary>
        public static bool IsBasicOrNullableType(this Type type)
        {
            return
                IsBasicType(type) ||
                Nullable.GetUnderlyingType(type) != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullableType(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullableType(this Type type, out Type underlyingType)
        {
            underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            return underlyingType != type;
        }

        public static bool IsNumericType(this Type type)
        {
            if (IsIntegerType(type))
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

        public static bool IsIntegerType(this Type type)
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

        /// <summary>
        /// Checks whether given type or its underlying nullable type is an enumeration type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsEnumType(this Type type)
        {
            return type.GetNonNullableType().IsEnum;
        }

        public static bool IsStructType(this Type type)
        {
            return type.IsValueType && !type.IsBasicType();
        }

        public static bool IsPlainObjectType(this Type type)
        {
            return type.IsClass && !type.IsSequenceType() && !type.IsBasicOrNullableType();
        }

        /// <summary>
        /// Checks whether a type is compiler generated.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is compiler generated; False otherwise.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompilerGenerated(this Type type)
        {
            return type.IsDefined(typeof(CompilerGeneratedAttribute), false);
        }

        /// <summary>
        /// Checks whether a type is a pre-compiled razor view.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a pre-compiled razor view; False otherwise.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRazorCompiledItem(this Type type)
        {
            return type.IsDefined(typeof(RazorCompiledItemAttribute), false);
        }

        /// <summary>
        /// Checks whether a given type is a delegate type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a delegate; false otherwise.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDelegate(this Type type)
        {
            return type.IsSubclassOf(typeof(Delegate));
        }

        [DebuggerStepThrough]
        public static bool IsAnonymousType(this Type type)
        {
            if (type.IsGenericType)
            {
                var d = type.GetGenericTypeDefinition();
                if (d.IsClass && d.IsSealed && d.Attributes.HasFlag(TypeAttributes.NotPublic))
                {
                    var attributes = d.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false);
                    if (attributes != null && attributes.Length > 0)
                    {
                        // WOW! We have an anonymous type!!!
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the given type is an array or implements <see cref="IEnumerable"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a sequence type; False otherwise.</returns>
        public static bool IsSequenceType(this Type type)
        {
            if (type.IsBasicOrNullableType())
            {
                return false;
            }

            return
                type.IsArray ||
                typeof(IEnumerable).IsAssignableFrom(type) ||
                // i.e., a direct ref to System.Array
                type == typeof(Array);
        }

        /// <summary>
        /// Checks whether the given type is an array or implements <see cref="IEnumerable"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="elementType">
        /// The generic argument type of the sequence, or <see cref="typeof(object)"/>
        /// if the sequence is non-generic.
        /// </param>
        /// <returns>True if the type is a sequence type; False otherwise.</returns>
        public static bool IsSequenceType(this Type type, out Type elementType)
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

        /// <summary>
        /// Checks whether the given type implements <see cref="IEnumerable{}"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="elementType">The generic argument type of the sequence.</param>
        /// <returns>True if the type is an enumerable type; False otherwise.</returns>
        public static bool IsEnumerableType(this Type type, out Type elementType)
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

        /// <summary>
        /// Checks whether the given type implements <see cref="ICollection{}"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="elementType">The generic argument type of the collection.</param>
        /// <returns>True if the type is a collection type; False otherwise.</returns>
        public static bool IsCollectionType(this Type type, out Type elementType)
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
        public static bool IsDictionaryType(this Type type)
        {
            return type.IsClosedGenericTypeOf(typeof(IDictionary<,>));
        }

        public static bool IsDictionaryType(this Type type, out Type keyType, out Type valueType)
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

        #endregion

        #region Generics

        /// <summary>
        /// Gets either the underlying type of a <see cref="Nullable{T}" /> type
        /// or the given type itself if it is not nullable.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetNonNullableType(this Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        /// <summary>
        /// Determine whether a given type is an open generic.
        /// </summary>
        /// <param name="source">The input type, e.g. <see cref="IEnumerable{}"/></param>
        /// <returns>True if the type is an open generic; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOpenGeneric(this Type source)
        {
            return source.IsGenericTypeDefinition || source.ContainsGenericParameters;
        }

        /// <summary>
        /// Checks whether given type is a closed type of a given open generic type.
        /// </summary>
        /// <param name="source">The source type to check.</param>
        /// <param name="openGeneric">The open generic type to validate against.</param>
        /// <returns>True if <paramref name="source"/> is a closed type of <paramref name="openGeneric"/>. False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClosedGenericTypeOf(this Type source, Type openGeneric)
        {
            return GetClosedGenericTypesOf(source, openGeneric).Any();
        }

        /// <inheritdoc cref="IsClosedGenericTypeOf(Type, Type)"/>
        /// <param name="closedGeneric">The first matching closed type.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClosedGenericTypeOf(this Type source, Type openGeneric, out Type closedGeneric)
        {
            closedGeneric = GetClosedGenericTypesOf(source, openGeneric).FirstOrDefault();
            return closedGeneric != null;
        }

        /// <summary>
        /// Looks for interfaces on the <paramref name="source"/> type that closes the given <paramref name="openGeneric"/> interface type.
        /// </summary>
        /// <param name="source">The type that is being checked for the interface.</param>
        /// <param name="openGeneric">The open generic service type to locate.</param>
        /// <returns>Matching closed implementation types.</returns>
        public static IEnumerable<Type> GetClosedGenericTypesOf(this Type source, Type openGeneric)
        {
            if (!openGeneric.IsOpenGeneric())
            {
                return Enumerable.Empty<Type>();
            }

            return GetTypesAssignableFrom(source)
                .Where(t => !t.ContainsGenericParameters && t.IsGenericType && t.GetGenericTypeDefinition() == openGeneric);
        }

        #endregion

        #region Attributes

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetAttribute<TAttribute>(this ICustomAttributeProvider target, bool inherits, out TAttribute attribute) where TAttribute : Attribute
        {
            attribute = GetAttribute<TAttribute>(target, inherits);
            return attribute != null;
        }

        /// <summary>
        /// Returns single attribute from the type
        /// </summary>
        /// <typeparam name="TAttribute">Attribute to use</typeparam>
        /// <param name="target">Attribute provider</param>
        ///<param name="inherits"><see cref="MemberInfo.GetCustomAttributes(Type,bool)"/></param>
        /// <returns><em>Null</em> if the attribute is not found</returns>
        /// <exception cref="InvalidOperationException">If there are 2 or more attributes</exception>
        public static TAttribute GetAttribute<TAttribute>(this ICustomAttributeProvider target, bool inherits) where TAttribute : Attribute
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
        public static bool HasAttribute<TAttribute>(this ICustomAttributeProvider target, bool inherits) where TAttribute : Attribute
        {
            return target.IsDefined(typeof(TAttribute), inherits);
        }

        /// <summary>
        /// Given a particular MemberInfo, return the custom attributes of the
        /// given type on that member.
        /// </summary>
        /// <typeparam name="TAttribute">Type of attribute to retrieve.</typeparam>
        /// <param name="target">The member to look at.</param>
        /// <param name="inherits">True to include attributes inherited from base classes.</param>
        /// <returns>Array of found attributes.</returns>
        public static TAttribute[] GetAttributes<TAttribute>(this ICustomAttributeProvider target, bool inherits) where TAttribute : Attribute
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

        /// <summary>
        /// Given a particular MemberInfo, find all the attributes that apply to this
        /// member. Specifically, it returns the attributes on the type, then (if it's a
        /// property accessor) on the property, then on the member itself.
        /// </summary>
        /// <typeparam name="TAttribute">Type of attribute to retrieve.</typeparam>
        /// <param name="member">The member to look at.</param>
        /// <param name="inherits">true to include attributes inherited from base classes.</param>
        /// <returns>Array of found attributes.</returns>
        public static TAttribute[] GetAllAttributes<TAttribute>(this MemberInfo member, bool inherits)
            where TAttribute : Attribute
        {
            List<TAttribute> attributes = new();

            if (member.DeclaringType != null)
            {
                attributes.AddRange(GetAttributes<TAttribute>(member.DeclaringType, inherits));

                MethodBase methodBase = member as MethodBase;
                if (methodBase != null)
                {
                    PropertyInfo prop = GetPropertyFromMethod(methodBase);
                    if (prop != null)
                    {
                        attributes.AddRange(GetAttributes<TAttribute>(prop, inherits));
                    }
                }
            }

            attributes.AddRange(GetAttributes<TAttribute>(member, inherits));
            return attributes.ToArray();
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

        #endregion
    }

}
