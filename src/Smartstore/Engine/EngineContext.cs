namespace Smartstore.Engine
{
    /// <summary>
    /// Provides access to the singleton instance of the Smartstore engine.
    /// </summary>
    public class EngineContext
    {
        /// <summary>
        /// Gets the singleton Smartstore engine used to access Smartstore services.
        /// </summary>
        public static IEngine Current
        {
            get => Singleton<IEngine>.Instance;
        }
    }
}
