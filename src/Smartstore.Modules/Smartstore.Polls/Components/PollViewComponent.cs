using Microsoft.AspNetCore.Mvc;
using Smartstore.Polls.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.Polls.Components
{
    public class PollViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(PublicPollModel model)
        {
            return View(model);
        }
    }
}
