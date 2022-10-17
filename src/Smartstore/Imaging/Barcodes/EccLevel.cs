namespace Smartstore.Imaging.Barcodes
{
    /// <summary>
    /// QR code error correction level. 
    /// These define the tolerance levels for how much of the code can be lost before the code cannot be recovered.
    /// </summary>
    public enum EccLevel
    {
        /// <summary>
        /// Low error correction level. 7% may be lost before recovery is not possible.
        /// </summary>
        Low,
        /// <summary>
        /// Medium error correction level. 15% may be lost before recovery is not possible.
        /// </summary>
        Medium,
        /// <summary>
        /// Quartile error correction level. 25% may be lost before recovery is not possible.
        /// </summary>
        Quartile,
        /// <summary>
        /// High error correction level. 30% may be lost before recovery is not possible.
        /// </summary>
        High
    }
}
