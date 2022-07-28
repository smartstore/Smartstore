namespace Smartstore.Data.Hooks
{
    public class DefaultDbHookActivator
    {
        // TODO: Finalize
        private readonly Dictionary<Type, IDbSaveHook> _instances = new();
        
        public IDbSaveHook Activate(HookMetadata hook)
        {
            if (hook == null)
            {
                throw new ArgumentNullException(nameof(hook));
            }

            return _instances.GetOrAdd(hook.ImplType, key =>
            {
                return (IDbSaveHook)Activator.CreateInstance(hook.ImplType);
            });
        }
    }
}
