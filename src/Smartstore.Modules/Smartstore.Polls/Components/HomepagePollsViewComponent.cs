using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Rules.Filters;
using Smartstore.Polls.Extensions;
using Smartstore.Polls.Hooks;
using Smartstore.Polls.Models.Mappers;
using Smartstore.Polls.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.Polls.Components
{
    /// <summary>
    /// Component to render new section on the homepage.
    /// </summary>
    public class HomepagePollsViewComponent : SmartViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var languageId = Services.WorkContext.WorkingLanguage.Id;
            var cacheKey = string.Format(ModelCacheInvalidator.HOMEPAGE_POLLS_MODEL_KEY, languageId, storeId);
            var cachedModel = await Services.Cache.GetAsync(cacheKey, () =>
            {
                return Services.DbContext.Polls()
                    .AsNoTracking()
                    .Include(x => x.PollAnswers)
                    .ApplyStandardFilter(storeId, languageId)
                    .Where(x => x.ShowOnHomePage == true && x.PollAnswers.Count > 0)
                    .OrderBy(x => x.DisplayOrder)
                    .SelectAsync(async x =>
                    {
                        return await x.MapAsync(new { SetAlreadyVotedProperty = false });
                    })
                    .AsyncToList();
            });

            // "AlreadyVoted" property of "PollModel" object depends on the current customer. Let's update it.
            // But first we need to clone the cached model (the updated one should not be cached)
            var model = new List<PublicPollModel>();

            foreach (var p in cachedModel)
            {
                var pollModel = (PublicPollModel)p.Clone();
                pollModel.AlreadyVoted = await Services.DbContext.PollAnswers().GetAlreadyVotedAsync(pollModel.Id, Services.WorkContext.CurrentCustomer.Id);
                pollModel.SystemKeyword = "Homepage";
                model.Add(pollModel);
            }

            if (model.Count == 0)
            {
                return Empty();
            }
            
            return View(model);
        }
    }
}
