using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Messaging;
using Smartstore.Core.Messaging.Events;
using Smartstore.Events;
using Smartstore.Polls.Domain;

namespace Smartstore.Polls
{
    public class Events : IConsumer
    {
        private readonly SmartDbContext _db;

        public Events(SmartDbContext db)
        {
            _db = db;
        }

        public async Task HandleEventAsync(MessageModelPartMappingEvent message, 
            IEventPublisher eventPublisher, 
            MessageModelHelper messageModelHelper)
        {
            if (message.Source is PollVotingRecord part)
            {
                message.Result = CreateModelPart(part, message.MessageContext, messageModelHelper);
                await eventPublisher.PublishAsync(new MessageModelPartCreatedEvent<PollVotingRecord>(part, message.Result));
            }
        }

        private static object CreateModelPart(PollVotingRecord part, MessageContext messageContext, MessageModelHelper messageModelHelper)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                { "PollAnswerId", part.PollAnswerId },
                { "PollAnswerName", part.PollAnswer.Name },
                { "PollId", part.PollAnswer.PollId },
            };

            messageModelHelper.ApplyCustomerContentPart(m, part, messageContext);

            return m;
        }

        public async Task HandleEventAsync(GdprCustomerDataExportedEvent message, 
            IMessageModelProvider messageModelProvider)
        {
            var pollVotings = message.Customer.CustomerContent.OfType<PollVotingRecord>();

            if (pollVotings.Any())
            {
                var ignoreMemberNames = new string[] { "CustomerId", "UpdatedOn" };
                message.Result["PollVotings"] = await pollVotings.SelectAsync(x => messageModelProvider.CreateModelPartAsync(x, true, ignoreMemberNames)).AsyncToList();
            }
        }
    }
}
