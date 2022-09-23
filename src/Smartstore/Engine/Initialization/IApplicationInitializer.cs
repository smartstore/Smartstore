using Microsoft.AspNetCore.Http;

namespace Smartstore.Engine.Initialization
{
    /// <summary>
    /// An implementation of this interface is used to execute application initialization code
    /// like e.g. database seeding etc. during the very first HTTP request and very early in the request lifecycle.
    /// Implementaions does NOT need to be registered in service collection, they will be resolved as transient implicitly. 
    /// </summary>
    public interface IApplicationInitializer
    {
        /// <summary>
        /// Get the value to use to order initializer instances. The default is 0.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Performs initialization.
        /// </summary>
        Task InitializeAsync(HttpContext httpContext);

        /// <summary>
        /// Called when an error occurred and <see cref="ThrowOnError"/> is <c>false</c>.
        /// </summary>
        /// <param name="exception">The error</param>
        /// <param name="willRetry"><c>true</c> when current attempt count is less than <see cref="MaxAttempts"/>, <c>false</c> otherwise.</param>
        Task OnFailAsync(Exception exception, bool willRetry);

        /// <summary>
        /// Whether to throw any error and stop execution of subsequent initializers.
        /// If this is <c>false</c>, the initializer will be executed and <see cref="OnFailAsync(Exception, bool)"/> 
        /// will be invoked to give you the chance to do some logging or fix things.
        /// </summary>
        bool ThrowOnError { get; }

        /// <summary>
        /// The number of maximum execution attempts before this task is removed from the queue.
        /// Has no effect if <see cref="ThrowOnError"/> is <c>true</c>.
        /// </summary>
        int MaxAttempts { get; }
    }
}