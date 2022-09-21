using Microsoft.AspNetCore.Http;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Localization
{
    [Important]
    internal class LocalizedPropertyAuditHook : DbSaveHook<LocalizedProperty>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalizedPropertyAuditHook(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override HookResult OnInserting(LocalizedProperty entity, IHookedEntity entry)
        {
            if (entity.CreatedOnUtc == default)
            {
                entity.CreatedOnUtc = DateTime.UtcNow;
            }

            entity.CreatedBy ??= GetAuthorName();

            return HookResult.Ok;
        }

        protected override HookResult OnUpdating(LocalizedProperty entity, IHookedEntity entry)
        {
            var isDateModified = entry.IsPropertyModified(nameof(LocalizedProperty.UpdatedOnUtc));

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

            if (entry.IsPropertyModified(nameof(LocalizedProperty.LocaleValue))) 
            {
                entity.TranslatedOnUtc = null;
            }

            return HookResult.Ok;
        }

        private string GetAuthorName()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }
    }
}
