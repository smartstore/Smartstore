using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Data;
using Smartstore.Web.Components;

namespace Smartstore.Forums.Components
{
    /// <summary>
    /// Component to render active discussions on forum homepage.
    /// </summary>
    public class HomeActiveDiscussionsViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly ForumSettings _forumSettings;

        public HomeActiveDiscussionsViewComponent(SmartDbContext db, ForumSettings forumSettings)
        {
            _db = db;
            _forumSettings = forumSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return Empty();
            }

            await Task.Delay(1);


            return Empty();
        }
    }
}
