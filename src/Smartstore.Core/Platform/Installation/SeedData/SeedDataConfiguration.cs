using Smartstore.Core.Localization;

namespace Smartstore.Core.Installation
{
    public class SeedDataConfiguration
    {
        public string DefaultUserName { get; set; }
        public string DefaultUserPassword { get; set; }
        public Language Language { get; set; }
        public InvariantSeedData Data { get; set; }
        public bool SeedSampleData { get; set; } = true;
        public bool StoreMediaInDB { get; set; } = true;
        public Action<string> ProgressMessageCallback { get; set; } = (x) => { };
    }
}