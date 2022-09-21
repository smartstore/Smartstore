using Microsoft.AspNetCore.Http;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Localization
{
    [Important(HookImportance.Essential)]
    internal class LocaleStringResourceAuditHook : DbSaveHook<LocaleStringResource>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocaleStringResourceAuditHook(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override HookResult OnInserting(LocaleStringResource entity, IHookedEntity entry)
        {
            if (entity.CreatedOnUtc == default)
            {
                entity.CreatedOnUtc = DateTime.UtcNow;
            }

            entity.CreatedBy ??= GetAuthorName();

            return HookResult.Ok;
        }

        protected override HookResult OnUpdating(LocaleStringResource entity, IHookedEntity entry)
        {
            var isDateModified = entry.IsPropertyModified(nameof(LocaleStringResource.UpdatedOnUtc));

            if (entity.UpdatedOnUtc == null || !isDateModified)
            {
                entity.UpdatedOnUtc = DateTime.UtcNow;
            }

            if (entity.UpdatedBy == null || !isDateModified)
            {
                // We assume that the caller who modified the date also modified
                // the author. In this case, we don't want to
                // update the "UpdatedBy" property, because custom author
                // should have precedence.
                entity.UpdatedBy = GetAuthorName();
            }

            return HookResult.Ok;
        }

        private string GetAuthorName()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }
    }
}
