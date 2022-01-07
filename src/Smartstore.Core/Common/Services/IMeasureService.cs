namespace Smartstore.Core.Common.Services
{
    /// <summary>
    /// Measure dimension service interface
    /// </summary>
    public interface IMeasureService
    {
        /// <summary>
        /// Converts dimension
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="source">Source dimension</param>
        /// <param name="target">Target dimension</param>
        /// <param name="round">A value indicating whether a result should be rounded</param>
        /// <returns>Converted value</returns>
        Task<decimal> ConvertDimensionAsync(decimal quantity, MeasureDimension source, MeasureDimension target, bool round = true);

        /// <summary>
        /// Converts to primary measure dimension
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="source">Source dimension</param>
        /// <returns>Converted value</returns>
        Task<decimal> ConvertToPrimaryDimensionAsync(decimal quantity, MeasureDimension source);

        /// <summary>
        /// Converts from primary dimension
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="target">Target dimension</param>
        /// <returns>Converted value</returns>
        Task<decimal> ConvertFromPrimaryDimensionAsync(decimal quantity, MeasureDimension target);

        /// <summary>
        /// Converts weight
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="source">Source weight</param>
        /// <param name="target">Target weight</param>
        /// <param name="round">A value indicating whether a result should be rounded</param>
        /// <returns>Converted value</returns>
        Task<decimal> ConvertWeightAsync(decimal quantity, MeasureWeight source, MeasureWeight target, bool round = true);

        /// <summary>
        /// Converts to primary measure weight
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="source">Source weight</param>
        /// <returns>Converted value</returns>
        Task<decimal> ConvertToPrimaryWeightAsync(decimal quantity, MeasureWeight source);

        /// <summary>
        /// Converts from primary weight
        /// </summary>
        /// <param name="quantity">Quantity</param>
        /// <param name="target">Target weight</param>
        /// <returns>Converted value</returns>
        Task<decimal> ConvertFromPrimaryWeightAsync(decimal quantity, MeasureWeight target);
    }
}