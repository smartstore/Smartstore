using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public static partial class ProductQuantityInputMappingExtensions
    {
        public static Task MapQuantityInputAsync(this Product product, IQuantityInput to, int selectedQuantity)
        {
            Guard.NotNull(product);
            Guard.NotNull(to);

            var mapper = MapperFactory.GetMapper<Product, IQuantityInput>();
            return mapper.MapAsync(product, to, new { SelectedQuantity = selectedQuantity });
        }
    }

    public class ProductQuantityInputMapper : QuantityInputMapperBase<Product, IQuantityInput>
    {
        protected override Task MapCoreAsync(Product product, IQuantityInput model, dynamic parameters = null)
        {
            var selectedQuantity = (int)parameters.SelectedQuantity;

            model.EnteredQuantity = product.OrderMinimumQuantity > selectedQuantity ? product.OrderMinimumQuantity : selectedQuantity;
            model.MinOrderAmount = product.OrderMinimumQuantity;
            model.MaxOrderAmount = product.OrderMaximumQuantity;
            model.QuantityStep = product.QuantityStep;
            model.QuantityControlType = product.QuantityControlType;

            MapCustomQuantities(model, product.ParseAllowedQuantities());

            return Task.CompletedTask;
        }
    }
}
