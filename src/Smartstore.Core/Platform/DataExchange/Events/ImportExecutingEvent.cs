namespace Smartstore.Core.DataExchange.Import.Events
{
    /// <summary>
    /// An event that is fired before a data import.
    /// </summary>
    public class ImportExecutingEvent
    {
        public ImportExecutingEvent(ImportExecuteContext context)
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
