using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Polls.Extensions;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Polls.Controllers
{
    [Route("[area]/poll/{action=index}/{id?}")]
    public class PollAdminController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IStoreMappingService _storeMappingService;
        private readonly CustomerSettings _customerSettings;

        public PollAdminController(
            SmartDbContext db,
            ILanguageService languageService,
            IDateTimeHelper dateTimeHelper,
            IStoreMappingService storeMappingService,
            CustomerSettings customerSettings)
        {
            _db = db;
            _languageService = languageService;
            _dateTimeHelper = dateTimeHelper;
            _storeMappingService = storeMappingService;
            _customerSettings = customerSettings;
        }

        private async Task PreparePollModelAsync(PollModel model, Poll poll, bool excludeProperties)
        {
            Guard.NotNull(model, nameof(model));

            if (!excludeProperties)
            {
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(poll);
            }

            ViewBag.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;

            var allLanguages = _languageService.GetAllLanguages(true);
            ViewBag.AvailableLanguages = allLanguages
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();

            ViewBag.IsSingleLanguageMode = allLanguages.Count <= 1;

            if (poll != null)
            {
                ViewBag.PollId = poll.Id;
            }

            ViewBag.AvailableSystemKeywords = new List<SelectListItem>
            {
                new SelectListItem { Text = T("Plugins.CMS.Polls.Systemname.MyAccountMenu"), Value = "MyAccountMenu" },
                new SelectListItem { Text = T("Plugins.CMS.Polls.Systemname.Blog"), Value = "Blog" }
            };
        }
        
        #region Polls

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        public IActionResult List()
        {
            var allLanguages = _languageService.GetAllLanguages(true);
            ViewBag.IsSingleLanguageMode = allLanguages.Count <= 1;

            return View();
        }

        [HttpPost]
        [Permission(PollPermissions.Read)]
        public async Task<IActionResult> PollList(GridCommand command)
        {
            var polls = await _db.Polls()
                .AsNoTracking()
                .Include(x => x.Language)
                .OrderBy(x => x.DisplayOrder)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var pollModels = await polls
                .SelectAsync(async x =>
                {
                    var model = await MapperFactory.MapAsync<Poll, PollModel>(x);
                    if (x.StartDateUtc.HasValue)
                    {
                        model.StartDate = _dateTimeHelper.ConvertToUserTime(x.StartDateUtc.Value, DateTimeKind.Utc);
                    }

                    if (x.EndDateUtc.HasValue)
                    {
                        model.EndDate = _dateTimeHelper.ConvertToUserTime(x.EndDateUtc.Value, DateTimeKind.Utc);
                    }

                    model.LanguageName = x.Language.Name;
                    model.EditUrl = Url.Action(nameof(Edit), "Poll", new { id = x.Id });

                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<PollModel>
            {
                Rows = pollModels,
                Total = await polls.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [Permission(PollPermissions.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new PollModel
            {
                Published = true,
                ShowOnHomePage = true
            };

            await PreparePollModelAsync(model, null, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(PollPermissions.Create)]
        public async Task<IActionResult> Create(PollModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var poll = await MapperFactory.MapAsync<PollModel, Poll>(model);
                poll.StartDateUtc = model.StartDate;
                poll.EndDateUtc = model.EndDate;

                _db.Polls().Add(poll);
                await _db.SaveChangesAsync();

                await SaveStoreMappingsAsync(poll, model.SelectedStoreIds);

                NotifySuccess(T("Admin.ContentManagement.Polls.Added"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = poll.Id }) : RedirectToAction(nameof(List));
            }

            // If we got this far something failed. Redisplay form.
            await PreparePollModelAsync(model, null, true);
            return View(model);
        }

        [Permission(PollPermissions.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var poll = await _db.Polls().FindByIdAsync(id, false);
            if (poll == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<Poll, PollModel>(poll);
            model.StartDate = poll.StartDateUtc;
            model.EndDate = poll.EndDateUtc;

            await PreparePollModelAsync(model, poll, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(PollPermissions.Update)]
        public async Task<IActionResult> Edit(PollModel model, bool continueEditing)
        {
            var poll = await _db.Polls().FindByIdAsync(model.Id);
            if (poll == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, poll);
                poll.StartDateUtc = model.StartDate;
                poll.EndDateUtc = model.EndDate;

                await SaveStoreMappingsAsync(poll, model.SelectedStoreIds);

                NotifySuccess(T("Admin.ContentManagement.Polls.Updated"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = poll.Id }) : RedirectToAction(nameof(List));
            }

            // If we got this far something failed. Redisplay form.
            await PreparePollModelAsync(model, poll, true);
            return View(model);
        }

        [HttpPost]
        [Permission(PollPermissions.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var poll = await _db.Polls().FindByIdAsync(id);
            if (poll == null)
            {
                return NotFound();
            }

            _db.Polls().Remove(poll);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.ContentManagement.Polls.Deleted"));
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(PollPermissions.Delete)]
        public async Task<IActionResult> DeleteSelection(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var polls = await _db.Polls().GetManyAsync(ids, true);

                _db.Polls().RemoveRange(polls);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion

        #region Poll answer

        [HttpPost]
        [Permission(PollPermissions.Read)]
        public async Task<IActionResult> PollAnswerList(int? pollId, GridCommand command)
        {
            var answers = await _db.PollAnswers()
                .AsNoTracking()
                .Where(x => x.PollId == pollId)
                .OrderBy(x => x.DisplayOrder)
                .ToPagedList(command.Page - 1, command.PageSize)
                .LoadAsync();

            var answersModel = answers.Select(x =>
            {
                return new PollAnswerModel
                {
                    Id = x.Id,
                    PollId = x.PollId,
                    Name = x.Name,
                    NumberOfVotes = x.NumberOfVotes,
                    DisplayOrder1 = x.DisplayOrder
                };
            });

            var gridModel = new GridModel<PollAnswerModel>
            {
                Rows = answersModel,
                Total = await answers.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(PollPermissions.EditAnswer)]
        public async Task<IActionResult> PollAnswerInsert(PollAnswerModel model, int pollId)
        {
            var success = false;

            if (!await _db.PollAnswers().AnyAsync(x => x.Name == model.Name && x.PollId == pollId))
            {
                _db.PollAnswers().Add(new PollAnswer
                {
                    PollId = pollId,
                    Name = model.Name,
                    DisplayOrder = model.DisplayOrder1
                });

                await _db.SaveChangesAsync();
                success = true;
            }
            else
            {
                NotifyError(T("Admin.CMS.Polls.NoDuplicatesAllowed"));
            }

            return Json(new { success });
        }

        [Permission(PollPermissions.EditAnswer)]
        public async Task<IActionResult> PollAnswerUpdate(PollAnswerModel model)
        {
            var success = false;
            var pollAnswers = await _db.PollAnswers().FindByIdAsync(model.Id);

            if (!ModelState.IsValid)
            {
                var modelStateErrors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage);
                return Content(modelStateErrors.FirstOrDefault());
            }

            if (pollAnswers != null)
            {
                await MapperFactory.MapAsync(model, pollAnswers);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(PollPermissions.Delete)]
        public async Task<IActionResult> PollAnswerDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var pollAnswers = await _db.PollAnswers().GetManyAsync(ids, true);

                _db.PollAnswers().RemoveRange(pollAnswers);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion

        #region Voting records

        [HttpPost]
        [Permission(PollPermissions.Read)]
        public async Task<IActionResult> PollVotesList(int pollId, GridCommand command)
        {
            var votings = await _db.CustomerContent
                //.AsNoTracking()                       // INFO (mh) (core): Very weird problem. With AsNoTracking Customers won't be loaded :-/
                .AsQueryable()
                .OfType<PollVotingRecord>()
                .ApplyPollFilter(pollId)
                .Include(x => x.Customer)
                .Include(x => x.PollAnswer)
                .ToPagedList(command.Page - 1, command.PageSize)
                .LoadAsync();

            var votingModel = votings.Select(x =>
            {
                return new PollVotingRecordModel
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    IsGuest = x.Customer.IsGuest(),
                    CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                    AnswerName = x.PollAnswer.Name,
                    Email = x.Customer.Email,
                    Username = x.Customer.Username,
                    FullName = x.Customer.GetFullName(),
                    CustomerEditUrl = Url.Action("Edit", "Customer", new { id = x.CustomerId })
                };
            });

            var gridModel = new GridModel<PollVotingRecordModel>
            {
                Rows = votingModel,
                Total = await votings.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        #endregion
    }
}
