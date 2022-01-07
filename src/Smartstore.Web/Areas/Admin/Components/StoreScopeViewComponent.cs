using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models;
using Smartstore.Core.Stores;

namespace Smartstore.Admin.Components
{
    public class StoreScopeViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var allStores = Services.StoreContext.GetAllStores();
            if (allStores.Count < 2)
            {
                return Empty();
            }

            var model = new StoreScopeConfigurationModel
            {
                StoreId = GetActiveStoreScopeConfiguration()
            };

            foreach (var store in allStores)
            {
                model.AllStores.Add(new SelectListItem
                {
                    Text = store.Name,
                    Selected = store.Id == model.StoreId,
                    Value = Url.Action("ChangeStoreScopeConfiguration", "Setting", new { storeid = store.Id, returnUrl = Request.RawUrl() })
                });
            }

            model.AllStores.Insert(0, new SelectListItem
            {
                Text = Services.Localization.GetResource("Admin.Common.StoresAll"),
                Selected = 0 == model.StoreId,
                Value = Url.Action("ChangeStoreScopeConfiguration", "Setting", new { storeid = 0, returnUrl = Request.RawUrl() })
            });

            return View(model);
        }

        private int GetActiveStoreScopeConfiguration()
        {
            var storeId = Services.WorkContext.CurrentCustomer.GenericAttributes.AdminAreaStoreScopeConfiguration;
            var store = Services.StoreContext.GetStoreById(storeId);
            return store != null ? store.Id : 0;
        }
    }
}
