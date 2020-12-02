using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Core.Common.Services
{
    public partial class GenericAttributeService : IGenericAttributeService
    {
        // TODO: (core) Implement GenericAttributeService.

        public TProp GetAttribute<TProp>(string entityName, int entityId, string key, int storeId = 0)
        {
            return default;
        }

        public Task<TProp> GetAttributeAsync<TProp>(string entityName, int entityId, string key, int storeId = 0)
        {
            return Task.FromResult(default(TProp));
        }

        public void ApplyAttribute<TProp>(int entityId, string key, string keyGroup, TProp value, int storeId = 0)
        {
            // ...
        }

        public Task ApplyAttributeAsync<TProp>(int entityId, string key, string keyGroup, TProp value, int storeId = 0)
        {
            return Task.CompletedTask;
        }
    }
}
