namespace Smartstore.Core.DataExchange.Import.Events
{
    /// <summary>
    /// An event that is fired after a data import.
    /// </summary>
    public class ImportExecutedEvent
    {
        public ImportExecutedEvent(ImportExecuteContext context)
        {
            Guard.NotNull(context, nameof(context));

            Context = context;
        }

        /// <summary>
        /// Context of the import.
        /// </summary>
        public ImportExecuteContext Context { get; private set; }
    }
}