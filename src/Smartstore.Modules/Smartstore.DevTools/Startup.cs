using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Smartstore.Engine.Builders;

namespace Smartstore.DevTools
{
    internal class Startup : StarterBase
    {
        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            //builder.MapRoutes(0, routes => 
            //{
            //    //routes.MapControllerRoute("SmartStore.DevTools",
            //    //     "Module/Smartstore.DevTools/{action=Configure}/{id?}"
            //    //);
            //});
        }
    }
}
