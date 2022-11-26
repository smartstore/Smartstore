using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace Smartstore.ComponentModel
{
    public class SmartContractResolver : DefaultContractResolver
    {
        protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            // .NET 7 native reflection is ultra-fast, even faster than
            // Newtonsoft's ExpressionValueProvider. As long as the devs
            // does not refactor their code, we gonna return ReflectionValueProvider here.
            return new ReflectionValueProvider(member);
        }

        public static SmartContractResolver Instance { get; } = new SmartContractResolver();
    }
}
