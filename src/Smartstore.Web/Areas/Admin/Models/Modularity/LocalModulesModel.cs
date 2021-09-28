using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Admin.Models.Stores;
using Smartstore.Collections;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Modularity
{
    public class LocalModulesModel : ModelBase
    {
        public List<StoreModel> AvailableStores { get; set; }

        public Multimap<string, ModuleModel> Groups { get; set; } = new();

        public ICollection<ModuleModel> AllModules => Groups.SelectMany(k => k.Value).AsReadOnly();
    }
}
