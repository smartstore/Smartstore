using Smartstore.Core.Common.Settings;
using Smartstore.Scheduling;

namespace Smartstore.Core.Identity.Tasks
{
    /// <summary>
    /// A task that periodically deletes guest customers.
    /// </summary>
    public partial class DeleteGuestsTask : ITask
    {
        private readonly ICustomerService _customerService;
        private readonly CommonSettings _commonSettings;

        public DeleteGuestsTask(ICustomerService customerService, CommonSettings commonSettings)
        {
            _customerService = customerService;
            _commonSettings = commonSettings;
        }

        public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            Guard.NotNegative(_commonSettings.MaxGuestsRegistrationAgeInMinutes, nameof(_commonSettings.MaxGuestsRegistrationAgeInMinutes));

            var registrationTo = DateTime.UtcNow.AddMinutes(-_commonSettings.MaxGuestsRegistrationAgeInMinutes);

            await _customerService.DeleteGuestCustomersAsync(null, registrationTo, true, cancelToken);
        }
    }
}
