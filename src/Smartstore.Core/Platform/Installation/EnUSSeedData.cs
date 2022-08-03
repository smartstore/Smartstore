using Smartstore.Core.Configuration;

namespace Smartstore.Core.Installation
{
    public class EnUSSeedData : InvariantSeedData
    {
        protected override void Alter(IList<ISettings> settings)
            => base.Alter(settings);
    }
}
