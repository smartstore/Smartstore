using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class ImportFile
    {
        public ImportFile(IFile file)
        {
            Guard.NotNull(file, nameof(file));

            File = file;

            var fileName = file.NameWithoutExtension;
            if (fileName.HasValue())
            {
                foreach (RelatedEntityType type in Enum.GetValues(typeof(RelatedEntityType)))
                {
                    if (fileName.EndsWith(type.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        RelatedType = type;
                    }
                }
            }
        }

        public IFile File { get; private set; }

        /// <summary>
        /// Related entity type.
        /// </summary>
        public RelatedEntityType? RelatedType { get; private set; }

        /// <summary>
        /// File label text.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets a value indicating whether the file has an CSV file extension.
        /// </summary>
        internal bool IsCsv => (new string[] { ".csv", ".txt", ".tab" }).Contains(File.Extension, StringComparer.OrdinalIgnoreCase);
    }
}
