using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Smartstore.Engine
{
    /// <summary>
    /// Classes implementing this interface can serve as a portal for the 
    /// various services composing the Smartstore engine. Edit functionality, modules
    /// and implementations access most Smartstore functionality through this 
    /// interface.
    /// </summary>
    public interface IEngine
    {
        /// <summary>
        /// Global application environment
        /// </summary>
        IApplicationContext Application { get; }

        /// <summary>
        /// Provides access to the scoped services container.
        /// </summary>
        ScopedServiceContainer Scope { get; set; }

        /// <summary>
        /// Returns a value indicating whether the engine has been started (after all starters).
        /// </summary>
        bool IsStarted { get; set; }

        /// <summary>
        /// Returns a value indicating whether the engine has been initialized (after all starters AND initializers).
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Starts the application engine.
        /// </summary>
        /// <param name="application">Application context instance</param>
        /// <returns>
        /// A system starter implementation that configures core app layer
        /// and detects and executes module starters.
        /// </returns>
        IEngineStarter Start(IApplicationContext application);
    }

    public static class IEngineExtensions
    {
        /// <summary>
        /// Resolves a service from the current service scope.
        /// </summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ResolveService<T>(this IEngine engine) where T : class
        {
            return engine.Scope.Resolve<T>();
        }

        /// <summary>
        /// Resolves a named service from the current service scope.
        /// </summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ResolveNamedService<T>(this IEngine engine, string name) where T : class
        {
            return engine.Scope.ResolveNamed<T>(name);
        }

        /// <summary>
        /// Resolves a keyed service from the current service scope.
        /// </summary>
        /// <typeparam name="T">Type of service to resolve.</typeparam>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ResolveKeyedService<T>(this IEngine engine, object key) where T : class
        {
            return engine.Scope.ResolveKeyed<T>(key);
        }
    }
}
