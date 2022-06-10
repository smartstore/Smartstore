namespace Smartstore.Admin.Models.Modularity
{
    public class WidgetModel : ProviderModel, IActivatable
    {
        [LocalizedDisplay("Common.IsActive")]
        public bool IsActive { get; set; }

    }
}
