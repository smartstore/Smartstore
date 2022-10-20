using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;

namespace Smartstore.Core.Common.Services
{
    public partial class MeasureService : IMeasureService
    {
        private readonly SmartDbContext _db;
        private readonly MeasureSettings _measureSettings;

        public MeasureService(SmartDbContext db, MeasureSettings measureSettings)
        {
            _db = db;
            _measureSettings = measureSettings;
        }

        public virtual async Task<decimal> ConvertDimensionAsync(decimal quantity, MeasureDimension source, MeasureDimension target, bool round = true)
        {
            decimal result = quantity;

            if (result != decimal.Zero && source.Id != target.Id)
            {
                result = await ConvertToPrimaryDimensionAsync(result, source);
                result = await ConvertFromPrimaryDimensionAsync(result, target);
            }

            if (round)
                result = Math.Round(result, 2);

            return result;
        }

        public virtual async Task<decimal> ConvertToPrimaryDimensionAsync(decimal quantity, MeasureDimension source)
        {
            decimal result = quantity;
            var baseDimensionIn = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId);

            if (result != decimal.Zero && source.Id != baseDimensionIn.Id)
            {
                decimal exchangeRatio = source.Ratio;
                if (exchangeRatio == decimal.Zero)
                    throw new Exception(string.Format("Exchange ratio not set for dimension [{0}]", source.Name));
                result /= exchangeRatio;
            }

            return result;
        }

        public virtual async Task<decimal> ConvertFromPrimaryDimensionAsync(decimal quantity, MeasureDimension target)
        {
            decimal result = quantity;
            var baseDimensionIn = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId);

            if (result != decimal.Zero && target.Id != baseDimensionIn.Id)
            {
                decimal exchangeRatio = target.Ratio;
                if (exchangeRatio == decimal.Zero)
                    throw new Exception(string.Format("Exchange ratio not set for dimension [{0}]", target.Name));
                result *= exchangeRatio;
            }

            return result;
        }

        public virtual async Task<decimal> ConvertWeightAsync(decimal quantity, MeasureWeight source, MeasureWeight target, bool round = true)
        {
            decimal result = quantity;
            if (result != decimal.Zero && source.Id != target.Id)
            {
                result = await ConvertToPrimaryWeightAsync(result, source);
                result = await ConvertFromPrimaryWeightAsync(result, target);
            }

            if (round)
                result = Math.Round(result, 2);

            return result;
        }

        public virtual async Task<decimal> ConvertToPrimaryWeightAsync(decimal quantity, MeasureWeight source)
        {
            decimal result = quantity;
            var baseWeightIn = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId);
            if (result != decimal.Zero && source.Id != baseWeightIn.Id)
            {
                decimal exchangeRatio = source.Ratio;
                if (exchangeRatio == decimal.Zero)
                    throw new Exception(string.Format("Exchange ratio not set for weight [{0}]", source.Name));
                result /= exchangeRatio;
            }
            return result;
        }

        public virtual async Task<decimal> ConvertFromPrimaryWeightAsync(decimal quantity, MeasureWeight target)
        {
            decimal result = quantity;
            var baseWeightIn = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId);

            if (result != decimal.Zero && target.Id != baseWeightIn.Id)
            {
                decimal exchangeRatio = target.Ratio;
                if (exchangeRatio == decimal.Zero)
                    throw new Exception(string.Format("Exchange ratio not set for weight [{0}]", target.Name));
                result *= exchangeRatio;
            }

            return result;
        }
    }
}
