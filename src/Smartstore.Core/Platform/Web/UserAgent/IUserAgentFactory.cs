namespace Smartstore.Core.Web
{
    /// <summary>
    /// Responsible for creating instances of <see cref="IUserAgent"/>
    /// </summary>
    public interface IUserAgentFactory
    {
        /// <summary>
        /// Creates a transient instance of <see cref="IUserAgent"/>
        /// for the given <paramref name="userAgent"/> string.
        /// </summary>
        /// <param name="userAgent">The user agent string to parse and materialize. Cannot be null.</param>
        /// <param name="enableCache">
        ///     <c>true</c>: try to load parsed <paramref name="userAgent"/> from memory cache first.
        ///     <c>false</c>: bypass cache and parse given <paramref name="userAgent"/>.
        ///  </param>
        /// <returns>An implementation of <see cref="IUserAgent"/>.</returns>
        IUserAgent CreateUserAgent(string userAgent, bool enableCache = true);
    }
}
