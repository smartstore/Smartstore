using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Engine.Initialization
{
    /// <summary>
    /// An implementation of this interface is used to execute application initialization code
    /// like e.g. database seeding etc. right before the very first HTTP request is being handled.
    /// Implementaions does NOT need to be registered in service collection, they will be resolved as transient implicitly. 
    /// A custom dependency scope will be spawned so that scoped dependencies can be passed via constructor, 
    /// but an instance of <see cref="HttpContext"/> will NOT be available.
    /// </summary>
    public interface IApplicationInitializer
    {
        /// <summary>
        /// Performs initialization.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Get the value to use to order initializer instances. The default is 0.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Whether to throw any error and stop execution of subsequent tasks.
        /// If this is <c>false</c>, the task will be executed and <see cref="OnFail(Exception, bool)"/> 
        /// will be invoked to give you the chance to do some logging or fix things.
        /// </summary>
        bool ThrowOnError { get; }

        /// <summary>
        /// Called when an error occurred and <see cref="ThrowOnError"/> is <c>false</c>.
        /// </summary>
        /// <param name="exception">The error</param>
        void OnFail(Exception exception);
    }
}