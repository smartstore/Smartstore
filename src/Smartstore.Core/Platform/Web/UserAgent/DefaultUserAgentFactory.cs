namespace Smartstore.Core.Web
{
    public class DefaultUserAgentFactory : IUserAgentFactory
    {
        private readonly IUserAgentParser _parser;

        public DefaultUserAgentFactory(IUserAgentParser parser) 
        {
            _parser = parser;
        }

        public IUserAgent2 CreateUserAgent(string userAgent)
        {
            return new DefaultUserAgent(userAgent, _parser);
        }
    }
}
