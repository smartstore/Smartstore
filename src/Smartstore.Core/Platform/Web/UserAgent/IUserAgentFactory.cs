namespace Smartstore.Core.Web
{
    /// <summary>
    /// Responsible for creating instances of <see cref="IUserAgent2"/>
    /// </summary>
    public interface IUserAgentFactory
    {
        /// <summary>
        /// Creates a transient instance of <see cref="IUserAgent2"/>
        /// for the given <paramref name="userAgent"/> string.
        /// </summary>
        /// <param name="userAgent">The user agent string to parse and materialize. Cannot be null.</param>
        /// <returns>An implementation of <see cref="IUserAgent2"/>.</returns>
        IUserAgent2 CreateUserAgent(string userAgent);
    }
}
