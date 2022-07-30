namespace Smartstore.Utilities
{
    public static class TypeHelper
    {
        public static Type GetElementType(Type type)
        {
            if (type.IsBasicType())
            {
                return type;
            }

            if (type.HasElementType)
            {
                return GetElementType(type.GetElementType());
            }

            if (type.IsNullableType(out var underlyingType))
            {
                return underlyingType;
            }

            if (type.IsSequenceType(out var elemenType))
            {
                return GetElementType(elemenType);
            }

            return type;
        }

        /// <summary>
        /// Exctracts and returns the name of a property accessor lambda
        /// </summary>
        /// <typeparam name="T">The containing type</typeparam>
        /// <param name="propertyAccessor">The accessor lambda</param>
        /// <param name="includeTypeName">When <c>true</c>, returns the result as '[TyoeName].[PropertyName]'.</param>
        /// <returns>The property name</returns>
        public static string NameOf<T>(Expression<Func<T, object>> propertyAccessor, bool includeTypeName = false)
        {
            Guard.NotNull(propertyAccessor, nameof(propertyAccessor));

            var name = propertyAccessor.ExtractPropertyInfo().Name;

            if (includeTypeName)
            {
                return typeof(T).Name + '.' + name;
            }

            return name;
        }
    }
}