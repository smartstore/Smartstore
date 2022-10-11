using Smartstore.Core.Configuration;

namespace Smartstore.Web.Api
{
    public class WebApiSettings : ISettings
    {
        /// <summary>
        /// The URL path prefix used to provide all Swagger documents.
        /// </summary>
        public const string SwaggerRoutePrefix = "docs/api";

        /// <summary>
        /// Gets the max value of $top that a client can request.
        /// Gets or sets a value indicating whether the Web API is active.
        /// </summary>
        public const int DefaultMaxTop = 120;

        /// <summary>
        /// Gets or sets a value indicating whether the Web API is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the max value of $top that a client can request.
        /// </summary>
        public int MaxTop { get; set; } = DefaultMaxTop;

        /// <summary>
        /// Gets or sets the max expansion depth for the $expand query option.
        /// </summary>
        public int MaxExpansionDepth { get; set; } = 8;

        #region Batch

        /// <summary>
        /// Gets or sets the maximum depth of nesting allowed when reading or writing recursive batch payloads.
        /// </summary>
        public int MaxBatchNestingDepth { get; set; } = 8;

        /// <summary>
        /// Gets or sets the maximum number of operations allowed in a single batch changeset.
        /// </summary>
        public int MaxBatchOperationsPerChangeset { get; set; } = 20;

        /// <summary>
        /// Gets or sets the maximum data size (in KB) that should be read from a batch message.
        /// </summary>
        public long MaxBatchReceivedMessageSize { get; set; } = 500;

        #endregion
    }
}
