using Autofac;

namespace Smartstore.Engine
{
    public class Work<T> where T : class
    {
        private readonly Func<T> _resolver;
        private readonly ILifetimeScopeAccessor _scopeAccessor;

        public Work(ILifetimeScopeAccessor scopeAccessor)
        {
            _scopeAccessor = scopeAccessor;
        }

        internal Work(Func<T> resolver)
        {
            // For unit tests
            _resolver = resolver;
        }

        public T Value
        {
            get
            {
                return _resolver != null
                    ? _resolver()
                    : _scopeAccessor.LifetimeScope.Resolve<T>();
            }
        }
    }
}
