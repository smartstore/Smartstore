using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Rules.Filters;
using Smartstore.News.Hooks;
using Smartstore.Polls;
using Smartstore.Polls.Extensions;
using Smartstore.Polls.Models.Mappers;
using Smartstore.Polls.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.News.Components
{
    /// <summary>
    /// Component to render new section on the homepage.
    /// </summary>
    public class HomepagePollsViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ICacheFactory _cacheFactory;
        
        public HomepagePollsViewComponent(
            SmartDbContext db,
            ICommonServices services,
            ICacheFactory cacheFactory)
        {
            _db = db;
            _services = services;
            _cacheFactory = cacheFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var storeId = _services.StoreContext.CurrentStore.Id;
            var languageId = _services.WorkContext.WorkingLanguage.Id;
            var cacheKey = string.Format(ModelCacheInvalidator.HOMEPAGE_POLLS_MODEL_KEY, languageId, storeId);
            var cachedModel = await _cacheFactory.GetMemoryCache().GetAsync(cacheKey, () =>
            {
                return _db.Polls()
                    .AsNoTracking()
                    .ApplyStandardFilter(storeId, languageId)
                    .Where(x => x.ShowOnHomePage == true)
                    .SelectAsync(async x =>
                    {
                        return await x.MapAsync(new { SetAlreadyVotedProperty = false });
                    })
                    .AsyncToList();
            });

            //"AlreadyVoted" property of "PollModel" object depends on the current customer. Let's update it.
            //But first we need to clone the cached model (the updated one should not be cached)
            var model = new List<PublicPollModel>();

            foreach (var p in cachedModel)
            {
                var pollModel = (PublicPollModel)p.Clone();
                pollModel.AlreadyVoted = await _db.PollAnswers().GetAlreadyVoted(pollModel.Id, _services.WorkContext.CurrentCustomer.Id);
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
