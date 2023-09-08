namespace Smartstore.Core.DataExchange.Import
{
    public class ImportMessage
    {
        public ImportMessage(string message, 
            ImportMessageType messageType = ImportMessageType.Info,
            ImportMessageReason reason = ImportMessageReason.None)
        {
            Guard.NotEmpty(message);

            Message = message;
            MessageType = messageType;
            Reason = reason;
        }

        public ImportMessageType MessageType { get; }
        public ImportMessageReason Reason { get; }

        public string Message { get; }
        public string FullMessage { get; init; }

        public ImportRowInfo AffectedItem { get; init; }
        public string AffectedField { get; init; }

        public override string ToString()
        {
            var appendix = AffectedItem != null
                ? $"Pos: {AffectedItem.Position + 1}"
                : null;

            if (AffectedField.HasValue())
                appendix = appendix.Grow("Field: " + AffectedField, ", ");

            if (appendix != null)
                return $"{Message.NaIfEmpty()} [{appendix}]";

            return Message.NaIfEmpty();
        }
    }

    public class ImportRowInfo : Tuple<int, string>
    {
        public ImportRowInfo(int position, string entityName)
            : base(position, entityName)
        {
        }

        public int Position => Item1;
        public string EntityName => Item2;
    }

    public enum ImportMessageType
    {
        Info = 0,
        Warning = 5,
        Error = 10
    }

    public enum ImportMessageReason
    {
        None = 0,
        DownloadFailed,
        EqualFile,
        EqualFileInAlbum
    }
}
