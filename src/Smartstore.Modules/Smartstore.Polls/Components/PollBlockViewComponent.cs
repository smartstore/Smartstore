using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Polls.Extensions;
using Smartstore.Polls.Hooks;
using Smartstore.Polls.Models.Mappers;
using Smartstore.Polls.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.Polls.Components
{
    public class PollBlockViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;

        public PollBlockViewComponent(SmartDbContext db, ICommonServices services)
        {
            _db = db;
            _services = services;
        }

        public async Task<IViewComponentResult> InvokeAsync(string systemKeyword)
        {
            var storeId = _services.StoreContext.CurrentStore.Id;
            var languageId = _services.WorkContext.WorkingLanguage.Id;
            
            var cacheKey = string.Format(ModelCacheInvalidator.POLL_BY_SYSTEMNAME_MODEL_KEY, systemKeyword, languageId, storeId);
            var cachedPolls = await _services.Cache.GetAsync(cacheKey, async () =>
            {
                var polls = await _db.Polls()
                    .AsNoTracking()
                    .ApplyStandardFilter(storeId, languageId)
                    .Where(x => x.PollAnswers.Count > 0)
                    .Include(x => x.PollAnswers)
                    .ToListAsync();

                var list = new List<PublicPollModel>(polls.Count);

                foreach (var poll in polls)
                {
                    list.Add(await poll.MapAsync(new { SetAlreadyVotedProperty = false }));
                }

                return list;
            });

            if (cachedPolls.Count == 0)
            {
                return Empty();
            }

            var model = new List<PublicPollModel>(cachedPolls.Count);

            // "AlreadyVoted" property of "PollModel" object depends on the current customer. Let's update it.
            // But first we need to clone the cached model (the updated one should not be cached).
            foreach (var cachedPoll in cachedPolls)
            {
                var clonedPoll = (PublicPollModel)cachedPoll.Clone();
                clonedPoll.AlreadyVoted = await _db.PollAnswers().GetAlreadyVotedAsync(cachedPoll.Id, _services.WorkContext.CurrentCustomer.Id);
                model.Add(clonedPoll);
            }

            return View(cachedPolls);
        }
    }
}