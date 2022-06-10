namespace Smartstore.Core.DataExchange.Import
{
    /// <summary>
    /// Data importer contract.
    /// </summary>
    public partial interface IDataImporter
    {
        /// <summary>
        /// Starts importing data. An <see cref="IEntityImporter"/> is responsible to save data to the database.
        /// </summary>
        /// <param name="request">Contains request data for importing.</param>
        /// <param name="cancelToken">A cancellation token to cancel the import.</param>
        Task ImportAsync(DataImportRequest request, CancellationToken cancelToken = default);
    }

    /// <summary>
    /// Entity importer contract.
    /// </summary>
    public partial interface IEntityImporter
    {
        /// <summary>
        /// Processes a batch of import data and saves it to the database.
        /// </summary>
        /// <param name="context">Contains all information to process import data.</param>
        Task ExecuteAsync(ImportExecuteContext context, CancellationToken cancelToken);
    }
}
