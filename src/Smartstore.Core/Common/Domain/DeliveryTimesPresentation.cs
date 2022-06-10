namespace Smartstore.Core.Common
{
    /// <summary>
    /// Represents how to present delivery times.
    /// </summary>
    public enum DeliveryTimesPresentation
    {
        /// <summary>
        /// Do not display.
        /// </summary>
        None = 0,

        /// <summary>
        /// Display label only.
        /// </summary>
        LabelOnly = 5,

        /// <summary>
        /// Display date only (if possible).
        /// </summary>
        DateOnly = 10,

        /// <summary>
        /// Display label and date.
        /// </summary>
        LabelAndDate = 15
    }
}
