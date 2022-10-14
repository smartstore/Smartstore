namespace Smartstore.Imaging.Barcodes
{
    /// <summary>
    /// Represents barcode payload.
    /// </summary>
    public class BarcodePayload : IEquatable<BarcodePayload>
    {
        public BarcodePayload(string data, BarcodeType type)
        {
            Guard.NotEmpty(data, nameof(data));
            
            Data = data;
            Type = type;
        }

        /// <summary>
        /// The string data to encode.
        /// </summary>
        public virtual string Data { get; }

        /// <summary>
        /// The barcode type to generate.
        /// </summary>
        public BarcodeType Type { get; }

        /// <summary>
        /// Whether the current barcode type is any EAN type.
        /// </summary>
        public bool IsEan
        {
            get => Type is (BarcodeType.Ean or BarcodeType.UPCA or BarcodeType.UPCE);
        }

        public override string ToString()
            => Data;

        public bool Equals(BarcodePayload other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            } 

            return GetType() == other.GetType() &&
                Type == other.Type &&
                Data == other.Data;
        }

        public override bool Equals(object obj)
            => Equals(obj as QrPayload);

        public override int GetHashCode()
            => Data.GetHashCode();
    }
}
