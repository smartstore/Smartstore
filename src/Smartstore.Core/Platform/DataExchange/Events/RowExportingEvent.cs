namespace Smartstore.Core.DataExchange.Export.Events
{
    // TODO: Another event message must be implemented, say 'ColumnsBuildingEvent'
    // The consumer of this event (most likely a plugin) could push a list of specific column headers
    // into the global export definition.

    public class RowExportingEvent
    {
        public dynamic Row { get; init; }

        public ExportEntityType EntityType { get; init; }

        public DataExportRequest ExportRequest { get; init; }

        public ExportExecuteContext ExecuteContext { get; init; }
    }
}
