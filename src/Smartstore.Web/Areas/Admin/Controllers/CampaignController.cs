using Smartstore.Admin.Models.Messages;
using Smartstore.ComponentModel;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class CampaignController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICampaignService _campaignService;
        private readonly IMessageModelProvider _messageModelProvider;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IAclService _aclService;

        public CampaignController(
            SmartDbContext db,
            ICampaignService campaignService,
            IMessageModelProvider messageModelProvider,
            IStoreMappingService storeMappingService,
            IAclService aclService)
        {
            _db = db;
            _campaignService = campaignService;
            _messageModelProvider = messageModelProvider;
            _storeMappingService = storeMappingService;
            _aclService = aclService;
        }

        private async Task PrepareCampaignModelAsync(CampaignModel model, Campaign campaign)
        {
            if (campaign != null)
            {
                model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(campaign.CreatedOnUtc, DateTimeKind.Utc);
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(campaign);
                model.SelectedCustomerRoleIds = await _aclService.GetAuthorizedCustomerRoleIdsAsync(campaign);
            }

            model.LastModelTree = await _messageModelProvider.GetLastModelTreeAsync(MessageTemplateNames.SystemCampaign);
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Promotion.Campaign.Read)]
        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Currency.Read)]
        public async Task<IActionResult> CampaignList(GridCommand command)
        {
            var campaigns = await _db.Campaigns
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<Campaign, CampaignModel>();
            var campaignModels = await campaigns
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    model.EditUrl = Url.Action(nameof(Edit), "Campaign", new { id = x.Id });
                    model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);

                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<CampaignModel>
            {
                Rows = campaignModels,
                Total = await campaigns.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Promotion.Campaign.Delete)]
        public async Task<IActionResult> CampaignDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var campaigns = await _db.Campaigns.GetManyAsync(ids, true);

                _db.Campaigns.RemoveRange(campaigns);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [Permission(Permissions.Promotion.Campaign.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new CampaignModel();
            await PrepareCampaignModelAsync(model, null);

            return View(model);
        }


        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Promotion.Campaign.Create)]
        public async Task<IActionResult> Create(CampaignModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var campaign = await MapperFactory.MapAsync<CampaignModel, Campaign>(model);
                campaign.CreatedOnUtc = DateTime.UtcNow;
                _db.Campaigns.Add(campaign);
                await _db.SaveChangesAsync();

                await SaveAclMappingsAsync(campaign, model.SelectedCustomerRoleIds);
                await SaveStoreMappingsAsync(campaign, model.SelectedStoreIds);

                NotifySuccess(T("Admin.Promotions.Campaigns.Added"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = campaign.Id }) : RedirectToAction(nameof(List));
            }

            await PrepareCampaignModelAsync(model, null);

            return View(model);
        }

        [Permission(Permissions.Promotion.Campaign.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var campaign = await _db.Campaigns.FindByIdAsync(id, false);
            if (campaign == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<Campaign, CampaignModel>(campaign);
            await PrepareCampaignModelAsync(model, campaign);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Promotion.Campaign.Update)]
        public async Task<IActionResult> Edit(CampaignModel model, bool continueEditing)
        {
            var campaign = await _db.Campaigns.FindByIdAsync(model.Id);
            if (campaign == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, campaign);
                await _db.SaveChangesAsync();

                await SaveAclMappingsAsync(campaign, model.SelectedCustomerRoleIds);
                await SaveStoreMappingsAsync(campaign, model.SelectedStoreIds);

                NotifySuccess(T("Admin.Promotions.Campaigns.Updated"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = campaign.Id }) : RedirectToAction(nameof(List));
            }

            await PrepareCampaignModelAsync(model, campaign);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.System.Message.Send)]
        public async Task<IActionResult> SendCampaign(CampaignModel model)
        {
            var campaign = await _db.Campaigns.FindByIdAsync(model.Id, false);
            if (campaign == null)
            {
                return NotFound();
            }

            try
            {
                var numberOfQueuedMessages = await _campaignService.SendCampaignAsync(campaign);

                NotifySuccess(T("Admin.Promotions.Campaigns.MassEmailSentToCustomers", numberOfQueuedMessages));
            }
            catch (Exception ex)
            {
                NotifyError(ex, false);
            }

            return RedirectToAction(nameof(Edit), new { id = model.Id });
        }

        [HttpPost]
        [Permission(Permissions.Promotion.Campaign.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var campaign = await _db.Campaigns.FindByIdAsync(id);
            if (campaign == null)
            {
                return NotFound();
            }

            _db.Campaigns.Remove(campaign);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Promotions.Campaigns.Deleted"));
            return RedirectToAction(nameof(List));
        }
    }
}
