using Smartstore.Admin.Models.Export;

namespace Smartstore.Admin.Components
{
    public class ExportProfileInfoViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;

        public ExportProfileInfoViewComponent(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync(string providerSystemName, string returnUrl = default)
        {
            Guard.NotEmpty(providerSystemName, nameof(providerSystemName));

            var profiles = await _db.ExportProfiles
                .Include(x => x.Task)
                .Include(x => x.Deployments)
                .AsNoTracking()
                .Where(x => x.ProviderSystemName == providerSystemName)
                .ToListAsync();

            var model = new ProfileInfoForProviderModel
            {
                ProviderSystemName = providerSystemName,
                ReturnUrl = returnUrl
            };

            model.Profiles = profiles
                .OrderBy(x => x.Enabled)
                .Select(x => new ProfileInfoForProviderModel.ProfileModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Enabled = x.Enabled,
                    TaskId = x.Enabled ? x.TaskId : null
                })
                .ToList();

            return View(model);
        }
    }
}
