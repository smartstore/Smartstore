#nullable enable

using System.Dynamic;

namespace Smartstore.ComponentModel
{
    /// <summary>
    /// A simple <see cref="DynamicObject"/> implementaion that does not throw
    /// when accessed member was not found.
    /// </summary>
    public class GracefulDynamicObject : DynamicObject
    {
        private readonly Dictionary<string, object?> _data;

        public GracefulDynamicObject(bool ignoreCase = false)
        {
            _data = ignoreCase ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) : [];
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _data.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            Guard.NotNull(binder);

            _data.TryGetValue(binder.Name, out result);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            Guard.NotNull(binder);

            _data[binder.Name] = value;
            return true;
        }
    }
}
