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

        public const int BeforeRewriteMiddleware = -755;
        public const int RewriteMiddleware = -750;
        public const int AfterRewriteMiddleware = -745;

        public const int BeforeStaticFilesMiddleware = -705;
        public const int StaticFilesMiddleware = -700;
        public const int AfterStaticFilesMiddleware = -695;

        public const int BeforeRoutingMiddleware = -655;
        public const int RoutingMiddleware = -650;
        public const int AfterRoutingMiddleware = -645;

        public const int BeforeAuthMiddleware = -605;
        public const int AuthMiddleware = -600;
        public const int AfterAuthMiddleware = -595;

        public const int BeforeWorkContextMiddleware = -555;
        public const int WorkContextMiddleware = -550;
        public const int AfterWorkContextMiddleware = -545;

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
