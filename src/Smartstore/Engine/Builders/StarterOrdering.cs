using System;

namespace Smartstore.Engine.Builders
{
    /// <summary>
    /// Some predefined defaults for the numerical ordering of starters, middleware and route registrations.
    /// </summary>
    public static class StarterOrdering
    {
        // Starters
        public const int First = -1000;
        public const int Early = -500;
        public const int Default = 0;
        public const int Late = 500;
        public const int Last = 1000;

        // Middlewares
        /// <summary>
        /// Not used by Smartstore
        /// </summary>
        public const int FirstMiddleware = -1000;
        public const int BeforeStaticFilesMiddleware = -705;
        public const int StaticFilesMiddleware = -700;
        public const int AfterStaticFilesMiddleware = -695;
        public const int BeforeRoutingMiddleware = -605;
        public const int RoutingMiddleware = -600;
        public const int AfterRoutingMiddleware = -595;
        public const int BeforeAuthorizationMiddleware = -555;
        public const int AuthorizationMiddleware = -550;
        public const int AfterAuthorizationMiddleware = -545;
        public const int EarlyMiddleware = -500;
        public const int DefaultMiddleware = 0;
        public const int LateMiddleware = 500;
        /// <summary>
        /// Not used by Smartstore
        /// </summary>
        public const int LastMiddleware = 1000;

        // Endpoints
        public const int FirstRoute = -1000;
        public const int EarlyRoute = -500;
        public const int DefaultRoute = 0;
        public const int LateRoute = 500;
        public const int LastRoute = 1000;
    }
}
