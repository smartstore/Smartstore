namespace Smartstore.Core.Search.Indexing
{
    public enum IndexOperationType
    {
        Index,
        Delete
    }

    /// <summary>
    /// Represents an indexing operation.
    /// </summary>
    public interface IIndexOperation
    {
        /// <summary>
        /// The type of the operation.
        /// </summary>
        IndexOperationType OperationType { get; }

        /// <summary>
        /// The document being inserted to or deleted from the index storage.
        /// </summary>
        IIndexDocument Document { get; }

        /// <summary>
        /// The database entity from which <see cref="Document"/> was created.
        /// </summary>
        BaseEntity Entity { get; }
    }

    public class IndexOperation : IIndexOperation
    {
        public IndexOperation(IIndexDocument document)
            : this(document, IndexOperationType.Index)
        {
        }

        public IndexOperation(IIndexDocument document, IndexOperationType type)
        {
            Guard.NotNull(document, nameof(document));

            Document = document;
            OperationType = type;
        }

        public IndexOperationType OperationType { get; }

        public BaseEntity Entity { get; set; }

        public IIndexDocument Document { get; }
    }
}
