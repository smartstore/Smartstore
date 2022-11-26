using System.Reflection;
using System.Runtime.Serialization;

namespace Smartstore.Templating.Liquid
{
    internal class ObjectDrop : SafeDropBase
    {
        private readonly object _data;
        private readonly Type _type;

        public ObjectDrop(object data)
        {
            Guard.NotNull(data, nameof(data));

            _data = data;
            _type = data.GetType();
        }

        public override bool ContainsKey(object key)
            => true;

        protected override object InvokeMember(string name)
        {
            var prop = _type.GetProperty(name);
            if (prop != null)
            {
                return prop.HasAttribute<IgnoreDataMemberAttribute>(true)
                    ? null
                    : prop.GetValue(_data);
            }

            var method = _type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
            if (method != null && method.GetParameters().Length == 0)
            {
                return method.Invoke(_data, null);
            }

            return null;
        }

        public override object GetWrappedObject()
            => _data;
    }
}
