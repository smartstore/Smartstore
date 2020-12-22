namespace Smartstore.Core.Catalog.Products
{
    public class AdjustInventoryResult
    {
        private int? _stockQuantityOld;
        private int? _stockQuantityNew;

        /// <summary>
        /// Gets or sets the stock quantity before adjustment.
        /// </summary>
        public int StockQuantityOld
        {
            get => _stockQuantityOld ?? 0;
            set => _stockQuantityOld = value;
        }

        /// <summary>
        /// Gets or sets the stock quantity after adjustment.
        /// </summary>
        public int StockQuantityNew
        {
            get => _stockQuantityNew ?? 0;
            set => _stockQuantityNew = value;
        }

        /// <summary>
        /// Gets a value indicating whether the adjustment resulted in a clear, unique stock quantity update. For instance <c>false</c> for bundle products.
        /// </summary>
        public bool HasClearStockQuantityResult => _stockQuantityOld.HasValue && _stockQuantityNew.HasValue;
    }
}
