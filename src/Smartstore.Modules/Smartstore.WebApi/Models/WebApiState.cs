namespace Smartstore.Web.Api.Models
{
    public class WebApiState
    {
        /// <summary>
        /// A value indicating whether the Web API is active.
        /// </summary>
        public bool IsActive { get; init; }

        /// <summary>
        /// The version of the Web API module.
        /// </summary>
        public string ModuleVersion { get; init; }

        /// <summary>
        /// The max value of $top that a client can request.
        /// </summary>
        public int MaxTop { get; init; }

        /// <summary>
        /// The max expansion depth for the $expand query option. If 0 then the maximum expansion depth check is disabled.
        /// </summary>
        public int MaxExpansionDepth { get; init; }

        /// <summary>
        /// Current API version.
        /// </summary>
        public string Version => $"1 {ModuleVersion}";
    }
}
