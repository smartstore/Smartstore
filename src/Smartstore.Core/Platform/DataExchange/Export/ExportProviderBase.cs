namespace Smartstore.Core.DataExchange.Export
{
    public abstract class ExportProviderBase : IExportProvider
    {
        /// <inheritdoc/>
        public virtual ExportEntityType EntityType => ExportEntityType.Product;

        /// <inheritdoc/>
        public virtual string FileExtension => null;

        /// <inheritdoc/>
        public virtual ExportConfigurationInfo ConfigurationInfo => null;

        /// <inheritdoc/>
        public Task ExecuteAsync(ExportExecuteContext context, CancellationToken cancelToken = default)
        {
            return ExportAsync(context, cancelToken);
        }

        /// <summary>
        /// Exports data to a file.
        /// </summary>
        /// <param name="context">Export execution context.</param>
        /// <param name="cancelToken">A cancellation token to cancel the export.</param>
        protected abstract Task ExportAsync(ExportExecuteContext context, CancellationToken cancelToken);

        /// <inheritdoc/>
        public virtual Task OnExecutedAsync(ExportExecuteContext context, CancellationToken cancelToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
