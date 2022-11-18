using Autofac;
using Autofac.Core;

namespace Smartstore.Data.Hooks
{
    public class DefaultDbHookActivator : IDbHookActivator
    {
        private readonly ILifetimeScope _scope;

        public DefaultDbHookActivator(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public virtual IDbSaveHook Activate(HookMetadata hook)
        {
            if (hook == null)
            {
                throw new ArgumentNullException(nameof(hook));
            }

            try
            {
                IDbSaveHook instance = null;

                for (var i = 0; i < hook.ServiceTypes.Length; i++)
                {
                    if (_scope.TryResolve(hook.ServiceTypes[i], out var obj) && obj is IDbSaveHook saveHook)
                    {
                        return saveHook;
                    }
                }

                if (instance == null)
                {
                    throw new DependencyResolutionException(
                        $"None of the provided service types [{string.Join(", ", hook.ServiceTypes.AsEnumerable())}] can resolve the hook '{hook.ImplType}'.");
                }

                return instance;
            }
            catch
            {
                throw;
            }
        }
    }
}
