namespace Smartstore.Admin.Models.Modularity
{
    public class AuthenticationMethodModel : ProviderModel, IActivatable
    {
        [LocalizedDisplay("Common.IsActive")]
        public bool IsActive { get; set; }
    }
}
