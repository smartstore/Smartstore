using System.Collections;
using DotLiquid;

namespace Smartstore.Templating.Liquid
{
    internal class EnumerableWrapper : IEnumerable<ILiquidizable>, ISafeObject
    {
        private readonly IEnumerable _enumerable;

        public EnumerableWrapper(IEnumerable enumerable)
        {
            _enumerable = Guard.NotNull(enumerable, nameof(enumerable));
        }

        public IEnumerator GetEnumerator()
            => GetEnumeratorInternal();

        public object GetWrappedObject()
            => _enumerable;

        IEnumerator<ILiquidizable> IEnumerable<ILiquidizable>.GetEnumerator()
            => GetEnumeratorInternal();

        private IEnumerator<ILiquidizable> GetEnumeratorInternal()
        {
            return _enumerable
                .Cast<object>()
                .Select(x => LiquidUtility.CreateSafeObject(x))
                .OfType<ILiquidizable>()
                .GetEnumerator();
        }
    }
}
