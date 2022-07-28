namespace Smartstore.Data.Hooks
{
    public enum HookStage
    {
        PreSave,
        PostSave
    }

    /// <summary>
    /// Database save hook registry & selector
    /// </summary>
    public interface IDbHookRegistry
    {
        /// <summary>
        /// Gets cached metadata for all registered hooks.
        /// </summary>
        /// <returns></returns>
        HookMetadata[] GetAllMetadata();

        /// <summary>
        /// Selects all hooks that can handle given <paramref name="entry"/> at specified <paramref name="stage"/>.
        /// </summary>
        /// <param name="entry">The entry to select all applicable hooks for.</param>
        /// <param name="stage">The hook stage.</param>
        /// <param name="minHookImportance">Minimum importance level of hooks to select.</param>
        /// <returns>Metadata of all applicable hooks</returns>
        HookMetadata[] SelectHooks(IHookedEntity entry, HookStage stage, HookImportance minHookImportance = HookImportance.Normal);

        /// <summary>
        /// Void hooks are known to be useless for a EntityType/EntityState/Stage combination.
        /// </summary>
        /// <param name="voidHook">The hook to register as void.</param>
        void RegisterVoidHook(HookMetadata voidHook, IHookedEntity entry, HookStage stage);
    }
}
