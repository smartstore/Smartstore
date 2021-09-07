using System.Threading.Tasks;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Events;
using Smartstore.Web.Modelling;
using Smartstore.Web.Rendering.Events;

namespace Smartstore.Forum.Events
{
    // TODO: (mg) (core) New convention in modules: declare event consumers
    // in "Events.cs" in module root. Split only if file gets too large.
    public class SettingsTabEventConsumer : IConsumer
    {
        public Localizer T { get; set; }

        public async Task HandleEventAsync(TabStripCreated message, IPermissionService permissions)
        {
            if (message.TabStripName.EqualsNoCase("searchsettings-edit"))
            {
                if (await permissions.AuthorizeAsync(ForumPermissions.Cms.Forum.Read))
                {
                    await message.TabFactory.AddAsync(builder => builder
                        .Text(T("Admin.Configuration.Settings.Forums"))
                        .Name("tab-search-forum")
                        .LinkHtmlAttributes(new { data_tab_name = "Forum" })
                        .Route(Module.SystemName, new { controller = "Forum", action = "SearchSettings", area = "Admin" })
                        .Ajax());
                }
            }
        }

        public Task HandleEventAsync(ModelBoundEvent message)
        {
            // TODO: (mg) (core) none of settings models is tabbable.
            // TODO: (mg) (core) ModelBoundEvent never published when saving settings.

            $"-- ModelBoundEvent".Dump();
            return Task.CompletedTask;
        }
    }
}
