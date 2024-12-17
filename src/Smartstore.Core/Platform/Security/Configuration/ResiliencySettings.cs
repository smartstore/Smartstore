using Smartstore.Core.Configuration;

namespace Smartstore.Core.Security
{
    public class ResiliencySettings : ISettings
    {
        public bool EnableOverloadProtection { get; set; } = false;

        public TimeSpan LongTrafficWindow { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan PeakTrafficWindow { get; set; } = TimeSpan.FromSeconds(2);

        public int? LongTrafficLimitCustomer { get; set; } = 500;
        public int? LongTrafficLimitGuest { get; set; } = 300;
        public int? LongTrafficLimitBot { get; set; } = 200;

        public int? PeakTrafficLimitCustomer { get; set; } = 50;
        public int? PeakTrafficLimitGuest { get; set; } = 20;
        public int? PeakTrafficLimitBot { get; set; } = 10;

        public int? LongTrafficLimitGlobal { get; set; } = 1000;
        public int? PeakTrafficLimitGlobal { get; set; } = 80;

        public bool ForbidNewGuestsIfAjaxOrPost { get; set; } = true;
    }
}
