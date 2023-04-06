using System.Text;

namespace Smartstore.Web.Api
{
    internal static class TypeExtensions
    {
        public static string GetFriendlyId(this Type type, bool fullyQualified = false)
        {
            Guard.NotNull(type);

            var typeName = fullyQualified
                ? type.FullNameSansTypeParameters().Replace('+', '.')
                : type.Name;

            if (type.IsGenericType)
            {
                var genericArgumentIds = type.GetGenericArguments()
                    .Select(t => t.GetFriendlyId(fullyQualified))
                    .ToArray();
                
                return new StringBuilder(typeName)
                    .Replace(string.Format("`{0}", genericArgumentIds.Length), string.Empty)
                    .Append(string.Format("[{0}]", string.Join(',', genericArgumentIds).TrimEnd(',')))
                    .ToString();
            }

            return typeName;
        }

        public static string FullNameSansTypeParameters(this Type type)
        {
            var fullName = type.FullName;
            if (string.IsNullOrEmpty(fullName))
            {
                fullName = type.Name;
            }

            var chopIndex = fullName.IndexOf("[[");
            return (chopIndex == -1) ? fullName : fullName[..chopIndex];
        }
    }
}
