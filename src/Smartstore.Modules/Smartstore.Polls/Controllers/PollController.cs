using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;
using Smartstore.Polls.Domain;
using Smartstore.Polls.Extensions;
using Smartstore.Polls.Models.Mappers;
using Smartstore.Web.Controllers;

namespace Smartstore.Polls.Controllers
{
    public class PollController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;

        public PollController(SmartDbContext db, IWorkContext workContext, IWebHelper webHelper)
        {
            _db = db;
            _workContext = workContext;
            _webHelper = webHelper;
        }

        [HttpPost]
        public async Task<IActionResult> Vote(int pollAnswerId)
        {
            var pollAnswer = await _db.PollAnswers().FindByIdAsync(pollAnswerId);

            if (pollAnswer == null)
            {
                return Json(new { error = T("Polls.AnswerNotFound", pollAnswerId).Value });
            }

            var poll = pollAnswer.Poll;

            if (!poll.Published)
            {
                return Json(new { error = T("Polls.NotAvailable").Value });
            }

            if (_workContext.CurrentCustomer.IsGuest() && !poll.AllowGuestsToVote)
            {
                return Json(new { error = T("Polls.OnlyRegisteredUsersVote").Value });
            }

            bool alreadyVoted = await _db.PollAnswers().GetAlreadyVoted(poll.Id, _workContext.CurrentCustomer.Id);
            if (!alreadyVoted)
            {
                //vote
                pollAnswer.PollVotingRecords.Add(new PollVotingRecord
                {
                    PollAnswerId = pollAnswer.Id,
                    CustomerId = _workContext.CurrentCustomer.Id,
                    IpAddress = _webHelper.GetClientIpAddress().ToString(),
                    IsApproved = true
                });

                //update totals
                pollAnswer.NumberOfVotes = pollAnswer.PollVotingRecords.Count;
                await _db.SaveChangesAsync();
            }

            var model = await poll.MapAsync(new { SetAlreadyVotedProperty = true });
            var widget = new ComponentWidgetInvoker("Poll", new { model });

            return new JsonResult(await InvokeWidgetAsync(widget));
        }
    }
}
