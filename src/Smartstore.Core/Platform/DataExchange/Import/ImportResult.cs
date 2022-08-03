namespace Smartstore.Core.DataExchange.Import
{
    public class ImportResult : ICloneable<SerializableImportResult>
    {
        public DateTime StartDateUtc { get; set; } = DateTime.UtcNow;

        public DateTime EndDateUtc { get; set; } = DateTime.UtcNow;

        public int TotalRecords { get; set; }

        public int SkippedRecords { get; set; }

        public int NewRecords { get; set; }

        public int ModifiedRecords { get; set; }

        public int AffectedRecords => NewRecords + ModifiedRecords;

        public bool Cancelled { get; set; }

        public IList<ImportMessage> Messages { get; private set; } = new List<ImportMessage>();

        public void Clear()
        {
            Messages.Clear();
            StartDateUtc = EndDateUtc = DateTime.UtcNow;
            TotalRecords = 0;
            SkippedRecords = 0;
            NewRecords = 0;
            ModifiedRecords = 0;
            Cancelled = false;
        }

        public ImportMessage AddInfo(string message, ImportRowInfo affectedRow = null, string affectedField = null)
            => AddMessage(message, ImportMessageType.Info, affectedRow, affectedField);

        public ImportMessage AddWarning(string message, ImportRowInfo affectedRow = null, string affectedField = null)
            => AddMessage(message, ImportMessageType.Warning, affectedRow, affectedField);

        public ImportMessage AddError(string message, ImportRowInfo affectedRow = null, string affectedField = null)
            => AddMessage(message, ImportMessageType.Error, affectedRow, affectedField);

        public ImportMessage AddMissingFieldError(ImportRowInfo affectedRow, string affectedEntity, string affectedField)
            => AddError($"The '{affectedField}' field is required for new {affectedEntity}. Skipping row.", affectedRow, affectedField);

        public ImportMessage AddError(Exception exception, string message)
            => AddMessage(message ?? exception.ToAllMessages(), ImportMessageType.Error, null, null, exception.StackTrace);

        public ImportMessage AddError(Exception exception, int? affectedBatch = null, string stage = null)
        {
            var prefix = new List<string>();

            if (affectedBatch.HasValue)
                prefix.Add("Batch: " + affectedBatch.Value);

            if (stage.HasValue())
                prefix.Add("Stage: " + stage);

            var msg = prefix.Any()
                ? "[{0}] {1}".FormatInvariant(string.Join(", ", prefix), exception.ToAllMessages())
                : exception.ToAllMessages();

            return AddMessage(msg, ImportMessageType.Error, fullMessage: exception.StackTrace);
        }

        public ImportMessage AddMessage(
            string message,
            ImportMessageType severity,
            ImportRowInfo affectedRow = null,
            string affectedField = null,
            string fullMessage = null)
        {
            var msg = new ImportMessage(message, severity)
            {
                AffectedItem = affectedRow,
                AffectedField = affectedField,
                FullMessage = fullMessage
            };

            Messages.Add(msg);
            return msg;
        }

        public bool HasWarnings =>
            Messages.Any(x => x.MessageType == ImportMessageType.Warning);

        public int Warnings =>
            Messages.Count(x => x.MessageType == ImportMessageType.Warning);

        public bool HasErrors
            => Messages.Any(x => x.MessageType == ImportMessageType.Error);

        public int Errors
            => Messages.Count(x => x.MessageType == ImportMessageType.Error);

        public string LastError
            => Messages.LastOrDefault(x => x.MessageType == ImportMessageType.Error)?.Message;

        object ICloneable.Clone()
        {
            return MemberwiseClone();
        }

        public SerializableImportResult Clone()
        {
            return new SerializableImportResult
            {
                StartDateUtc = StartDateUtc,
                EndDateUtc = EndDateUtc,
                TotalRecords = TotalRecords,
                SkippedRecords = SkippedRecords,
                NewRecords = NewRecords,
                ModifiedRecords = ModifiedRecords,
                AffectedRecords = AffectedRecords,
                Cancelled = Cancelled,
                Warnings = Warnings,
                Errors = Errors,
                LastError = LastError
            };
        }
    }

    [Serializable]
    public partial class SerializableImportResult
    {
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public int TotalRecords { get; set; }
        public int SkippedRecords { get; set; }
        public int NewRecords { get; set; }
        public int ModifiedRecords { get; set; }
        public int AffectedRecords { get; set; }
        public bool Cancelled { get; set; }
        public int Warnings { get; set; }
        public int Errors { get; set; }
        public string LastError { get; set; }
    }
}
