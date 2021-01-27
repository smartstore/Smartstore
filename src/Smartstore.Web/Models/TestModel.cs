using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models
{
    // TODO: (core) Remove TestModel later
    [LocalizedDisplayName("Common.")]
    public class TestModel : ModelBase
    {
        [LocalizedDisplayName("*Yes")]
        public string TestProp1 { get; set; }

        [LocalizedDisplayName("*No")]
        public string TestProp2 { get; set; }
    }
}
