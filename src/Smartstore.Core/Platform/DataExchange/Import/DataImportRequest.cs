using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class DataImportRequest
    {
        public DataImportRequest(int profileId)
        {
            Guard.NotZero(profileId, nameof(profileId));

            ProfileId = profileId;
            ProgressCallback = OnProgress;
        }

        public int ProfileId { get; private set; }

        public bool HasPermission { get; set; }

        public IList<int> EntitiesToImport { get; set; } = new List<int>();

        public ProgressCallback ProgressCallback { get; init; }

        public IDictionary<string, object> CustomData { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        Task OnProgress(int value, int max, string msg)
        {
            return Task.CompletedTask;
        }
    }
}
