using Smartstore.Scheduling;

namespace Smartstore.AmazonPay.Services
{
    public class DataPollingTask : ITask
    {
        private readonly IAmazonPayService _amazonPayService;

        public DataPollingTask(IAmazonPayService amazonPayService)
        {
            _amazonPayService = amazonPayService;
        }

        public Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            return _amazonPayService.RunDataPollingAsync(cancelToken);
        }
    }
}
