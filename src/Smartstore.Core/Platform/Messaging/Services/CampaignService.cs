using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
                    .ToArrayAsync();
            }

            while (true)
            {
                cancelToken.ThrowIfCancellationRequested();

                var subscribers = _db.NewsletterSubscriptions
                    .ApplyStandardFilter(null, false, storeIds, roleIds)
                    .ToPagedList(++pageIndex, 500);

                await foreach (var subscriber in subscribers)
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

                    var result = await CreateCampaignMessageAsync(campaign, subscriber);
                    if ((result?.Email?.Id ?? 0) != 0)
                    {
                        alreadyProcessedEmails.Add(subscriber.Subscription.Email);
                        ++totalEmailsSent;

                        // Publish event so that integrators can add attachments, alter the email etc.
                        await _eventPublisher.PublishAsync(new MessageQueuingEvent
                        {
                            QueuedEmail = result.Email,
                            MessageContext = result.MessageContext,
                            MessageModel = result.MessageContext.Model
                        });

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

        public virtual Task<CreateMessageResult> CreateCampaignMessageAsync(Campaign campaign, NewsletterSubscriber subscriber)
        {
            Guard.NotNull(campaign, nameof(campaign));

            if (subscriber?.Subscription == null)
            {
                return null;
            }

            var messageContext = new MessageContext
            {
                MessageTemplate = GetCampaignTemplate(),
                Customer = subscriber.Customer
            };

            return _messageFactory.CreateMessageAsync(messageContext, false /* do NOT queue */, subscriber.Subscription, campaign);
        }

        public virtual async Task<CreateMessageResult> PreviewAsync(Campaign campaign)
        {
            Guard.NotNull(campaign, nameof(campaign));

            var messageContext = new MessageContext
            {
                MessageTemplate = GetCampaignTemplate(),
                TestMode = true
            };

            var testModel = await _messageFactory.GetTestModelsAsync(messageContext);
            var subscription = testModel.OfType<NewsletterSubscription>().FirstOrDefault();

            return await _messageFactory.CreateMessageAsync(messageContext, false /* do NOT queue */, subscription, campaign);
        }

        private MessageTemplate GetCampaignTemplate()
        {
            var messageTemplate = _db.MessageTemplates
                .AsNoTracking()
                .Where(x => x.Name == MessageTemplateNames.SystemCampaign)
                .ApplyStoreFilter(_storeContext.CurrentStore.Id)
                .FirstOrDefault();

            if (messageTemplate == null)
                throw new SmartException(T("Common.Error.NoMessageTemplate", MessageTemplateNames.SystemCampaign));

            return messageTemplate;
        }
    }
}
