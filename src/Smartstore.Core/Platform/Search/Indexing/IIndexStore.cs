namespace Smartstore.Core.Search.Indexing
{
    public interface IIndexStore
    {
        /// <summary>
        /// Gets the index info.
        /// </summary>
        IndexInfo IndexInfo { get; }

        /// <summary>
        /// Checks whether the index is already existing or not.
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Creates a new index if it doesn't exist already.
        /// </summary>
        void CreateIfNotExists();

        /// <summary>
        /// Deletes the index.
        /// </summary>
        Task DeleteAsync();

        /// <summary>
        /// Gets the total number of indexed documents.
        /// </summary>
        /// <param name="documentType">Type of document, use <c>null</c> to get all documents. <see cref="SearchDocumentTypes"/> for known types.</param>
        int GetDocumentCount(string documentType);

        /// <summary>
        /// Gets the size of the index in bytes.
        /// </summary>
        Task<long> GetIndexSizeAsync();

        /// <summary>
        /// Returns every field's name available in the index.
        /// </summary>
        IEnumerable<string> GetAllFields();

        /// <summary>
        /// Removes all documents from the index.
        /// </summary>
        void Clear();

        /// <summary>
        /// Acquires an index writer.
        /// </summary>
        /// <param name="writerContext">Provides information about the indexing operation.</param>
        /// <remarks>
        /// This method creates a transient writer instance which automatically gets released on dispose.
        /// </remarks>
        IDisposable AcquireWriter(AcquireWriterContext writerContext);

        /// <summary>
        /// Adds a set of new documents to the index.
        /// </summary>
        /// <remarks>
        /// This method will delete already existing documents before saving them.
        /// </remarks>
        void SaveDocuments(IEnumerable<IIndexDocument> documents);

        /// <summary>
        /// Removes a set of existing documents from the index.
        /// </summary>
        void DeleteDocuments(IEnumerable<IIndexDocument> documents);

        /// <summary>
        /// Removes a set of existing documents from the index.
        /// </summary>
        /// <param name="documentType">Type of document. <see cref="SearchDocumentTypes"/> for known types.</param>
        void DeleteDocuments(string documentType);
    }

    public static class IIndexStoreExtensions
    {
        /// <summary>
        /// Adds a new document to the index.
        /// </summary>
        /// <remarks>
        /// This method will delete a document with the same entity id - if it exists - before saving it.
        /// </remarks>
        public static void SaveDocument(this IIndexStore store, IIndexDocument document)
        {
            store.SaveDocuments(new[] { document });
        }

        /// <summary>
        /// Removes an existing document from the index.
        /// </summary>
        public static void DeleteDocument(this IIndexStore store, IIndexDocument document)
        {
            store.DeleteDocuments(new[] { document });
        }
    }
}
