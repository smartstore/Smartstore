using System;

namespace Smartstore.Redis.Configuration
{
    public class RedisConfiguration
    {
        public class RedisConnectionStrings
        {
            public string Default { get; set; }
            public string Bus { get; set; }
            public string Cache { get; set; }
            public string OutputCache { get; set; }
            public string SessionStore { get; set; }
        }

        public RedisConnectionStrings ConnectionStrings { get; set; } = new RedisConnectionStrings();
    }
}
