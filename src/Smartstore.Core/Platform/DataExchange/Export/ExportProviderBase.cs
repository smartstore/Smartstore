using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Core.DataExchange.Export
{
    public abstract class ExportProviderBase : IExportProvider
    {
        /// <inheritdoc/>
        public ExportEntityType EntityType => ExportEntityType.Product;

        /// <inheritdoc/>
        public string FileExtension => null;

        /// <inheritdoc/>
        public ExportConfigurationInfo ConfigurationInfo => null;

        /// <inheritdoc/>
        public Task ExecuteAsync(ExportExecuteContext context, CancellationToken cancellationToken)
        {
            return ExportAsync(context, cancellationToken);
        }

        /// <summary>
        /// Exports data to a file.
        /// </summary>
        /// <param name="context">Export execution context.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the export.</param>
        protected abstract Task ExportAsync(ExportExecuteContext context, CancellationToken cancellationToken);

        /// <inheritdoc/>
        public virtual Task OnExecutedAsync(ExportExecuteContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
