using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var pollAnswer = await _db.PollAnswers()
                .Include(x => x.Poll)
                .ThenInclude(x => x.PollAnswers)
                .FindByIdAsync(pollAnswerId);

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

            bool alreadyVoted = await _db.PollAnswers().GetAlreadyVotedAsync(poll.Id, _workContext.CurrentCustomer.Id);
            if (!alreadyVoted)
            {
                pollAnswer.PollVotingRecords.Add(new PollVotingRecord
                {
                    PollAnswerId = pollAnswer.Id,
                    CustomerId = _workContext.CurrentCustomer.Id,
                    IpAddress = _webHelper.GetClientIpAddress().ToString(),
                    IsApproved = true
                });

                // Update totals
                pollAnswer.NumberOfVotes = pollAnswer.PollVotingRecords.Count;
                await _db.SaveChangesAsync();
            }

            var model = await poll.MapAsync(new { SetAlreadyVotedProperty = true });
            var widget = new ComponentWidgetInvoker("Poll", new { model });

            return Json(new
            {
                html = await InvokeWidgetAsync(widget)
            });
        }
    }
}
