using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Data.Hooks;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Services;

namespace Smartstore.Forums.Hooks
{
    internal class PrivateMessageHook : AsyncDbSaveHook<PrivateMessage>
    {
        private readonly SmartDbContext _db;
        private readonly IGenericAttributeService _genericAttributeService;

        public PrivateMessageHook(SmartDbContext db, IGenericAttributeService genericAttributeService)
        {
            _db = db;
            _genericAttributeService = genericAttributeService;
        }

        protected override Task<HookResult> OnInsertedAsync(PrivateMessage entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Reset customer's attribute so that the next time he enters his my-account area, he will be notified about new messages.
            var addedMessages = entries
                .Where(x => x.InitialState == Data.EntityState.Added)
                .Select(x => x.Entity)
                .OfType<PrivateMessage>()
                .ToList();

            if (addedMessages.Any())
            {
                var entityName = nameof(Customer);

                await _genericAttributeService.PrefetchAttributesAsync(entityName, addedMessages.ToDistinctArray(x => x.ToCustomerId));

                foreach (var pm in addedMessages)
                {
                    var attributes = _genericAttributeService.GetAttributesForEntity(entityName, pm.ToCustomerId);

                    attributes.Set(ForumService.NotifiedAboutNewPrivateMessagesKey, false, pm.StoreId);
                }

                await _db.SaveChangesAsync(cancelToken);
            }
        }
    }
}
