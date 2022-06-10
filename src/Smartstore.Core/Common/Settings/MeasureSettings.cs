using Smartstore.Core.Configuration;

namespace Smartstore.Core.Common.Settings
{
    public class MeasureSettings : ISettings
    {
        public int BaseDimensionId { get; set; }
        public int BaseWeightId { get; set; }
    }
}
