namespace Smartstore.Web.Api.Models.Media
{
    public partial class CheckUniquenessResult
    {
        /// <summary>
        /// True when passed path exists already.
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// The new unique path if the passed path already exists, otherwise null.
        /// </summary>
        public string NewPath { get; set; }
    }
}
