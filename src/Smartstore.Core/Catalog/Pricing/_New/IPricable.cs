using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing
{
    public interface IPricable
    {
        Task<decimal> GetRegularPriceAsync();
    }
}
