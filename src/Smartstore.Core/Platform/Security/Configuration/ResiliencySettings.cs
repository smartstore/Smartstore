using Smartstore.Core.Configuration;

namespace Smartstore.Core.Security
{
    public class ResiliencySettings : ISettings
    {
        public bool EnableOverloadProtection { get; set; } = false;

        public TimeSpan LongTrafficWindow { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan PeakTrafficWindow { get; set; } = TimeSpan.FromSeconds(2);

        public int? LongTrafficLimitGuest { get; set; } = 600;
        public int? LongTrafficLimitBot { get; set; } = 400;

        public int? PeakTrafficLimitGuest { get; set; } = 50;
        public int? PeakTrafficLimitBot { get; set; } = 30;

        public int? LongTrafficLimitGlobal { get; set; } = 1000;
        public int? PeakTrafficLimitGlobal { get; set; } = 80;

        public bool ForbidNewGuestsIfAjaxOrPost { get; set; } = true;
    }
}
