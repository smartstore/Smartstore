namespace Smartstore.Data.Hooks
{
    /// <summary>
    /// Responsible for creating hook implementation instances.
    /// </summary>
    public interface IDbHookActivator
    {
        /// <summary>
        /// Creates or resolves an instance of <see cref="HookMetadata.ImplType"/>
        /// </summary>
        /// <param name="hook">The hook metadata</param>
        /// <returns>The implementation instance</returns>
        IDbSaveHook Activate(HookMetadata hook);
    }

    /// <summary>
    /// For unit-test purposes
    /// </summary>
    internal class SimpleDbHookActivator : IDbHookActivator
    {
        private readonly Dictionary<HookMetadata, IDbSaveHook> _instances = new();

        public IDbSaveHook Activate(HookMetadata hook)
        {
            return _instances.GetOrAdd(hook, key =>
            {
                return (IDbSaveHook)Activator.CreateInstance(hook.ImplType);
            });
        }
    }
}
