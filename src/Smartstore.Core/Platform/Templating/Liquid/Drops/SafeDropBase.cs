using DotLiquid;

namespace Smartstore.Templating.Liquid
{
    internal interface ISafeObject
    {
        object GetWrappedObject();
    }

    internal abstract class SafeDropBase : ILiquidizable, IIndexable, ISafeObject
    {
        private readonly Dictionary<string, object> _safeObjects = new();

        protected object GetOrCreateSafeObject(string name)
        {
            if (!_safeObjects.TryGetValue(name, out var safeObject))
            {
                safeObject = LiquidUtility.CreateSafeObject(InvokeMember(name));
                if (safeObject is ISafeObject)
                {
                    _safeObjects[name] = safeObject;
                }
            }

            return safeObject;
        }

        public abstract bool ContainsKey(object key);

        public object this[object key] => (key is string s) ? GetOrCreateSafeObject(s) : null;

        protected abstract object InvokeMember(string name);

        public object ToLiquid()
        {
            return this;
        }

        public abstract object GetWrappedObject();
    }
}
