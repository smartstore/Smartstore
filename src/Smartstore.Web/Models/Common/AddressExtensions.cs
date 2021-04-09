using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Smartstore.ComponentModel;
using Smartstore.Core.Common;
using Smartstore.Web.Models.Common;

namespace Smartstore
{
    public static class AddressExtensions
    {
        public static async Task MapAsync(this Address entity, AddressModel model, bool excludeProperties = false, List<Country> countries = null)
        {
            dynamic parameters = new ExpandoObject();
            parameters.excludeProperties = excludeProperties;
            parameters.countries = countries;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }
}
