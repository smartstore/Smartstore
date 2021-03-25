using System.Collections.Generic;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data.Setup;

namespace Smartstore.Web.Infrastructure.Installation
{
    public class EnUSSeedData : InvariantSeedData
    {
        protected override void Alter(IList<ISettings> settings) 
            => base.Alter(settings);
    }
}
