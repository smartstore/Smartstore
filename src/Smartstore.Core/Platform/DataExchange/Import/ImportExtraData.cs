namespace Smartstore.Core.DataExchange.Import
{
    [Serializable]
    public partial class ImportExtraData
    {
        /// <summary>
        /// Number of images per object to be imported.
        /// </summary>
        public int? NumberOfPictures { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether to update all records that match a key field value.
        /// </summary>
        public bool UpdateAllKeyFieldMatches { get; set; }
    }
}
