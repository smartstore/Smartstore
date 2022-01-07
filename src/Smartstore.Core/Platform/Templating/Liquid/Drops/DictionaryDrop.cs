namespace Smartstore.Templating.Liquid
{
    internal class DictionaryDrop : SafeDropBase
    {
        private readonly IDictionary<string, object> _inner;

        public DictionaryDrop(IDictionary<string, object> data)
        {
            _inner = Guard.NotNull(data, nameof(data));
        }

        public override bool ContainsKey(object key)
        {
            return (key is string s)
                ? _inner.ContainsKey(s)
                : false;
        }

        protected override object InvokeMember(string name)
            => _inner.Get(name);

        public override object GetWrappedObject()
            => _inner;
    }
}
