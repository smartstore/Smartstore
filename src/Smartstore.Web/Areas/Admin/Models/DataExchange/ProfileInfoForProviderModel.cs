namespace Smartstore.Admin.Models.Export
{
    public class ProfileInfoForProviderModel : ModelBase
    {
        public string ProviderSystemName { get; set; }
        public string ReturnUrl { get; set; }

        [LocalizedDisplay("Admin.DataExchange.Export.ProfileForProvider")]
        public List<ProfileModel> Profiles { get; set; }

        public class ProfileModel : EntityModelBase
        {
            public int? TaskId { get; set; }
            public bool Enabled { get; set; }
            public string Name { get; set; }
        }
    }
}
