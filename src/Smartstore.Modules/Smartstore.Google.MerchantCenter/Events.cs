using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Events;
using Smartstore.Google.MerchantCenter.Domain;
using Smartstore.Google.MerchantCenter.Models;
using Smartstore.Web.Rendering.Events;

namespace Smartstore.Google.MerchantCenter
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

                await message.TabFactory.AppendAsync(builder => builder
                    .Text("GMC")
                    .Name("tab-gmc")
                    .Icon("google", "bi")
                    .LinkHtmlAttributes(new { data_tab_name = "GMC" })
                    .Action("ProductEditTab", "GoogleMerchantCenter", new { productId })
                    .Ajax());
            }
        }

        public async Task HandleEventAsync(ModelBoundEvent message)
        {
            if (!message.BoundModel.CustomProperties.ContainsKey("GMC"))
                return;

            var model = message.BoundModel.CustomProperties["GMC"] as GoogleProductModel;
            if (model == null)
                return;

            var utcNow = DateTime.UtcNow;
            var entity = await _db.GoogleProducts()
                .Where(x => x.ProductId == model.ProductId)
                .FirstOrDefaultAsync();

            var insert = entity == null;

            if (entity == null)
            {
                entity = new GoogleProduct
                {
                    ProductId = model.ProductId,
                    CreatedOnUtc = utcNow
                };
            }

            MiniMapper.Map(model, entity);
            entity.UpdatedOnUtc = utcNow;
            entity.IsTouched = entity.IsTouched();

            if (!insert && !entity.IsTouched)
            {
                _db.GoogleProducts().Remove(entity);
                await _db.SaveChangesAsync();
                return;
            }

            if (insert)
            {
                _db.GoogleProducts().Add(entity);
            }

            await _db.SaveChangesAsync();
        }
    }
}
