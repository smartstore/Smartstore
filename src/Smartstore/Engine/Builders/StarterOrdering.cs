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

        public const int BeforeExceptionHandlerMiddleware = -805;
        public const int ExceptionHandlerMiddleware = -800;
        public const int AfterExceptionHandlerMiddleware = -795;

        public const int BeforeStaticFilesMiddleware = -755;
        public const int StaticFilesMiddleware = -750;
        public const int AfterStaticFilesMiddleware = -745;

        public const int BeforeRoutingMiddleware = -705;
        public const int RoutingMiddleware = -700;
        public const int AfterRoutingMiddleware = -695;

        public const int BeforeAuthMiddleware = -655;
        public const int AuthMiddleware = -650;
        public const int AfterAuthMiddleware = -645;

        public const int BeforeWorkContextMiddleware = -605;
        public const int WorkContextMiddleware = -600;
        public const int AfterWorkContextMiddleware = -595;

        public const int BeforeRewriteMiddleware = -555;
        public const int RewriteMiddleware = -550;
        public const int AfterRewriteMiddleware = -545;

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
