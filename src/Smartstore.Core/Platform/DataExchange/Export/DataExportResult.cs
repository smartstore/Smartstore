using System.Xml.Serialization;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Export
{
    [Serializable]
    public class DataExportResult
    {
        /// <summary>
        /// A value indicating whether the export succeeded.
        /// </summary>
        public bool Succeeded => LastError.IsEmpty();

        /// <summary>
        /// Last error.
        /// </summary>
        [XmlIgnore]
        public string LastError { get; set; }

        /// <summary>
        /// Files created by last export.
        /// </summary>
        public List<ExportFileInfo> Files { get; set; } = new();

        /// <summary>
        /// The path of the folder with the export files.
        /// </summary>
        [XmlIgnore]
        public IDirectory ExportDirectory { get; set; }

        [Serializable]
        public class ExportFileInfo
        {
            /// <summary>
            /// Store identifier, can be 0.
            /// </summary>
            public int StoreId { get; set; }

            /// <summary>
            /// Name of the export file including file extension, e.g. "1-7-0001-ordercsvexport.csv".
            /// </summary>
            public string FileName { get; set; }

            /// <summary>
            /// Short optional text that describes the content of the file.
            /// </summary>
            public string Label { get; set; }

            /// <summary>
            /// The related entity type.
            /// </summary>
            public RelatedEntityType? RelatedType { get; set; }
        }
    }


    public class DataExportPreviewResult
    {
        /// <summary>
        /// Preview data.
        /// </summary>
        public List<dynamic> Data { get; set; } = new();

        /// <summary>
        /// Number of total records.
        /// </summary>
        public int TotalRecords { get; set; }
    }
}
