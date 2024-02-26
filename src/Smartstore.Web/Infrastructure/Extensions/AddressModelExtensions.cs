using Smartstore.Core.Identity;

namespace Smartstore.Web.Models.Common
{
    public static partial class AddressModelExtensions
    {
        public static IOrderedAsyncEnumerable<AddressModel> OrderByDefaultAddresses(this IAsyncEnumerable<AddressModel> models)
        {
            Guard.NotNull(models);

            return models.OrderByDescending(x =>
            {
                if (x.IsDefaultBillingAddress && x.IsDefaultShippingAddress)
                    return int.MaxValue;
                else if (x.IsDefaultBillingAddress)
                    return int.MaxValue - 1;
                else if (x.IsDefaultShippingAddress)
                    return int.MaxValue - 2;

                return x.Id;
            });
        }

        public static void ApplyDefaultAddresses(this AddressModel model, Customer customer)
        {
            Guard.NotNull(model);
            Guard.NotNull(customer);

            var ga = customer.GenericAttributes;

            if (customer.IsTransientRecord())
            {
                if (model.IsDefaultBillingAddress)
                {
                    ga.DefaultBillingAddressId = model.Id;
                }
                if (model.IsDefaultShippingAddress)
                {
                    ga.DefaultShippingAddressId = model.Id;
                }
            }
            else
            {
                if (ga.DefaultBillingAddressId == model.Id && !model.IsDefaultBillingAddress)
                {
                    ga.DefaultBillingAddressId = 0;
                }
                else if (ga.DefaultBillingAddressId != model.Id && model.IsDefaultBillingAddress)
                {
                    ga.DefaultBillingAddressId = model.Id;
                }

                if (ga.DefaultShippingAddressId == model.Id && !model.IsDefaultShippingAddress)
                {
                    ga.DefaultShippingAddressId = 0;
                }
                else if (ga.DefaultShippingAddressId != model.Id && model.IsDefaultShippingAddress)
                {
                    ga.DefaultShippingAddressId = model.Id;
                }
            }
        }
    }
}
