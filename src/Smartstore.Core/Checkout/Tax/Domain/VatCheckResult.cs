namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Represents the result of VAT number check.
    /// </summary>
    public partial class VatCheckResult
    {
        public VatCheckResult(VatNumberStatus status, string vatNumber)
        {
            Status = status;
            VatNumber = vatNumber;
        }

        public static implicit operator VatNumberStatus(VatCheckResult obj)
            => obj.Status;

        /// <summary>
        /// Gets or sets the VAT number status.
        /// </summary>
        public VatNumberStatus Status { get; private set; }

        /// <summary>
        /// Gets or sets the VAT number to be checked (including the two letter country ISO code).
        /// </summary>
        public string VatNumber { get; private set; }

        /// <summary>
        /// Gets or sets the two letter country ISO code of the <see cref="VatNumber"/> returned by the VAT service.
        /// </summary>
        public string CountryCode { get; init; }

        /// <summary>
        /// Gets or sets the name of the holder of the <see cref="VatNumber"/> returned by the VAT service.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Gets or sets the address of the holder of the <see cref="VatNumber"/> returned by the VAT service.
        /// </summary>
        public string Address { get; init; }

        /// <summary>
        /// Gets or sets an exception that occurred during the check of the <see cref="VatNumber"/>.
        /// <c>null</c> if the VAT service is operating properly.
        /// </summary>
        public Exception Exception { get; init; }

        public override string ToString()
        {
            return $"VAT:{VatNumber} {Status}. name:{Name} country:{CountryCode} address:{Address}";
        }
    }
}