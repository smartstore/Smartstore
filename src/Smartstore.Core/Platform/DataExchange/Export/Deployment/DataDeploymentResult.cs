namespace Smartstore.Core.DataExchange.Export.Deployment
{
    [Serializable]
    public class DataDeploymentResult
    {
        /// <summary>
        /// A value indicating whether the deployment succeeded.
        /// </summary>
        public bool Succeeded => LastError.IsEmpty();

        /// <summary>
        /// Gets or sets the last error.
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// Gets or sets the last execution date.
        /// </summary>
        public DateTime LastExecutionUtc { get; set; }
    }
}
