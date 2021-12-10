using Smartstore.Google.MerchantCenter.Domain;

namespace Smartstore.Google.MerchantCenter
{
    public static class GoogleProductExtensions
    {
        public static bool IsTouched(this GoogleProduct p)
        {
            if (p != null)
            {
                return
                    p.Taxonomy.HasValue() || p.Gender.HasValue() || p.AgeGroup.HasValue() || p.Color.HasValue() ||
                    p.Size.HasValue() || p.Material.HasValue() || p.Pattern.HasValue() || p.ItemGroupId.HasValue() ||
                    !p.Export || p.Multipack != 0 || p.IsBundle.HasValue || p.IsAdult.HasValue || p.EnergyEfficiencyClass.HasValue() ||
                    p.CustomLabel0.HasValue() || p.CustomLabel1.HasValue() || p.CustomLabel2.HasValue() || p.CustomLabel3.HasValue() || p.CustomLabel4.HasValue();
            }

            return false;
        }
    }
}
