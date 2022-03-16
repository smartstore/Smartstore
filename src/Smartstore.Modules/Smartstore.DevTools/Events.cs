using Smartstore.Core.Data;
using Smartstore.DevTools.Models;
using Smartstore.Events;
using Smartstore.Web.Rendering.Events;

namespace Smartstore.DevTools
{
    public class Events : IConsumer
    {
        private readonly SmartDbContext _db;

        public Events(SmartDbContext db)
        {
            _db = db;
        }

        public async Task HandleEventAsync(TabStripCreated message)
        {
            if (message.TabStripName == "product-edit")
            {
                var productId = ((TabbableModel)message.Model).Id;

                // add in a predefined tab "Plugins" which serves as container for plugins to obtain data 
                // TODO: (mh) (core) How to do this?

                // add in an own tab

                //await message.TabFactory.AddAsync(builder => builder.Text("Dev Tools")
                //    .Name("tab-dt")
                //    .Icon("fa fa-code fa-lg fa-fw")
                //    .LinkHtmlAttributes(new { data_tab_name = "DevTools" })
                //    .Action("ProductEditTab", "DevTools", new { productId })
                //    .Ajax());
            }
        }

        public void HandleEvent(ModelBoundEvent message)
        {
            if (!message.BoundModel.CustomProperties.ContainsKey("DevTools"))
                return;

            var model = message.BoundModel.CustomProperties["DevTools"] as BackendExtensionModel;
            if (model == null)
                return;

        }
    }
}