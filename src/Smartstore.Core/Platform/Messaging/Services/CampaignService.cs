using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging.Events;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Events;

namespace Smartstore.Core.Messaging
{
    public partial class CampaignService : ICampaignService
    {
        private readonly SmartDbContext _db;
        private readonly IMessageFactory _messageFactory;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IEventPublisher _eventPublisher;

        public CampaignService(
            SmartDbContext db,
            IMessageFactory messageFactory,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IEventPublisher eventPublisher)
        {
            _db = db;
            _messageFactory = messageFactory;
            _storeContext = storeContext;
            _storeMappingService = storeMappingService;
            _eventPublisher = eventPublisher;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual async Task<int> SendCampaignAsync(Campaign campaign, CancellationToken cancelToken = default)
        {
            Guard.NotNull(campaign, nameof(campaign));

            var totalEmailsSent = 0;
            var pageIndex = -1;
            int[] storeIds = null;
            int[] roleIds = null;
            var alreadyProcessedEmails = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var campaignTemplate = await GetCampaignTemplate(cancelToken);

            if (campaign.LimitedToStores)
            {
                storeIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(campaign);
            }

            if (campaign.SubjectToAcl)
            {
                roleIds = await _db.AclRecords
                    .ApplyEntityFilter(campaign)
                    .Select(x => x.CustomerRoleId)
                    .Distinct()
                    .ToArrayAsync(cancelToken);
            }

            while (true)
            {
                cancelToken.ThrowIfCancellationRequested();

                var subscribers = _db.NewsletterSubscriptions
                    .ApplyStandardFilter(null, false, storeIds, roleIds)
                    .ToPagedList(++pageIndex, 500);

                await foreach (var subscriber in subscribers.AsAsyncEnumerable())
                {
                    // Create only one message per subscription email.
                    if (alreadyProcessedEmails.Contains(subscriber.Subscription.Email))
                    {
                        continue;
                    }

                    if (subscriber.Customer != null && !subscriber.Customer.Active)
                    {
                        continue;
                    }

                    var messageContext = new MessageContext
                    {
                        MessageTemplate = campaignTemplate,
                        Customer = subscriber.Customer
                    };

                    var result = await _messageFactory.CreateMessageAsync(messageContext, false, subscriber.Subscription, campaign);
                    if (result.Email != null)
                    {
                        alreadyProcessedEmails.Add(subscriber.Subscription.Email);
                        ++totalEmailsSent;

                        // Publish event so that integrators can add attachments, alter the email etc.
                        await _eventPublisher.PublishAsync(new MessageQueuingEvent
                        {
                            QueuedEmail = result.Email,
                            MessageContext = result.MessageContext,
                            MessageModel = result.MessageContext.Model
                        }, cancelToken);

                        // Queue emails so they can be saved later in one go.
                        _db.QueuedEmails.Add(result.Email);
                    }
                }

                // Save all queued emails now.
                await _db.SaveChangesAsync(cancelToken);

                if (!subscribers.HasNextPage)
                {
                    break;
                }
            }

            return totalEmailsSent;
        }

        public virtual async Task<CreateMessageResult> CreateCampaignMessageAsync(Campaign campaign, NewsletterSubscriber subscriber)
        {
            Guard.NotNull(campaign, nameof(campaign));

            if (subscriber?.Subscription == null)
            {
                return null;
            }

            var messageContext = new MessageContext
            {
                MessageTemplate = await GetCampaignTemplate(),
                Customer = subscriber.Customer
            };

            return await _messageFactory.CreateMessageAsync(messageContext, false /* do NOT queue */, subscriber.Subscription, campaign);
        }

        public virtual async Task<CreateMessageResult> PreviewAsync(Campaign campaign)
        {
            Guard.NotNull(campaign, nameof(campaign));

            var messageContext = new MessageContext
            {
                MessageTemplate = await GetCampaignTemplate(),
                TestMode = true
            };

            var testModel = await _messageFactory.GetTestModelsAsync(messageContext);
            var subscription = testModel.OfType<NewsletterSubscription>().FirstOrDefault();

            return await _messageFactory.CreateMessageAsync(messageContext, false /* do NOT queue */, subscription, campaign);
        }

        private async Task<MessageTemplate> GetCampaignTemplate(CancellationToken cancelToken = default)
        {
            var messageTemplate = await _db.MessageTemplates
                .AsNoTracking()
                .Where(x => x.Name == MessageTemplateNames.SystemCampaign)
                .ApplyStoreFilter(_storeContext.CurrentStore.Id)
                .FirstOrDefaultAsync(cancelToken);

            if (messageTemplate == null)
            {
                throw new InvalidOperationException(T("Common.Error.NoMessageTemplate", MessageTemplateNames.SystemCampaign));
            }

            return messageTemplate;
        }
    }
}
