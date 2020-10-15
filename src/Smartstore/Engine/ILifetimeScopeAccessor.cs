using System;
using Autofac;

namespace Smartstore.Engine
{
    public interface ILifetimeScopeAccessor
    {
        /// <summary>Gets a nested lifetime scope that services can be resolved from.</summary>
        /// <returns>A new or existing nested lifetime scope.</returns>
        ILifetimeScope LifetimeScope { get; }

        /// <summary>
        /// Ends the current lifetime scope, but only when <c>HttpContext</c> is <c>null</c>.
        /// </summary>
        void EndLifetimeScope();

        /// <summary>
        ///		Either creates a new lifetime scope when <c>HttpContext</c> is <c>null</c>,
        ///		OR returns the current HTTP context scoped lifetime.
        /// </summary>
        /// <returns>
        ///		A disposable object which does nothing when internal lifetime scope is bound to the HTTP context,
        ///		OR ends the lifetime scope otherwise.
        /// </returns>
        /// <remarks>
        ///		This method is intended for usage in background threads or tasks. There may be situations where HttpContext is present,
        ///		especially when a task was started with <c>TaskScheduler.FromCurrentSynchronizationContext()</c>. In this case it may not be
        ///		desirable to create a new scope, but use the existing, HTTP context bound scope instead.
        /// </remarks>
        IDisposable BeginContextAwareScope();

        /// <summary>
        ///		Creates a new nested lifetime scope that services can be resolved from.
        /// </summary>
        /// <param name="configurationAction">
        ///     An optional configuration action that will execute during lifetime scope creation.
        /// </param>
        /// <returns>
        ///		The new nested lifetime scope.
        ///	</returns>
        ILifetimeScope BeginLifetimeScope(Action<ContainerBuilder> configurationAction = null);
    }
}