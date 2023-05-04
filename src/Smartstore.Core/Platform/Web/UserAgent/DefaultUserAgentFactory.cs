namespace Smartstore.Core.Web
{
    public class DefaultUserAgentFactory : IUserAgentFactory
    {
        private readonly IUserAgentParser _parser;

        public DefaultUserAgentFactory(IUserAgentParser parser) 
        {
            _parser = parser;
        }

        public IUserAgent CreateUserAgent(string userAgent, bool enableCache = true)
        {
            return new DefaultUserAgent(userAgent, enableCache, _parser);
        }
    }
}
