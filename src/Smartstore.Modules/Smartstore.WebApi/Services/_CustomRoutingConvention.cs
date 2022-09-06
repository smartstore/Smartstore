using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.OData.Edm;

namespace Smartstore.WebApi.Services
{
    //public class CustomRoutingConvention : IODataControllerActionConvention
    //{
    //    public int Order => 0;

    //    public bool AppliesToAction(ODataControllerActionContext context)
    //    {
    //        var action = context.Action;
    //        var entitySet = context.EntitySet;
    //        var entityType = entitySet.EntityType();

    //        return false;
    //    }

    //    public bool AppliesToController(ODataControllerActionContext context)
    //    {
    //        var action = context.Action;
    //        var entitySet = context.EntitySet;
    //        var entityType = entitySet.EntityType();


    //        return false;
    //    }
    //}


    // https://github.com/OData/AspNetCoreOData/issues/75
    // https://devblogs.microsoft.com/odata/routing-in-asp-net-core-8-0-preview/
    public class CustomRoutingConvention : EntitySetRoutingConvention
    {
        /// <inheritdoc />
        public override bool AppliesToController(ODataControllerActionContext context)
        {
            var action = context.Action;
            var entitySet = context.EntitySet;
            var entityType = entitySet.EntityType();

            return base.AppliesToController(context);
        }

        /// <inheritdoc />
        public override bool AppliesToAction(ODataControllerActionContext context)
        {
            var action = context.Action;
            var entitySet = context.EntitySet;
            var entityType = entitySet.EntityType();


            return base.AppliesToAction(context);
        }


    }
}
