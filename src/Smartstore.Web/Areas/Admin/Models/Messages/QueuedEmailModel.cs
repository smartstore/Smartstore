using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Messaging;

namespace Smartstore.Admin.Models.Messages
{
    [LocalizedDisplay("Admin.System.QueuedEmails.Fields.")]
    public class QueuedEmailModel : EntityModelBase
    {
        [LocalizedDisplay("*Id")]
        public override int Id { get; set; }

        [LocalizedDisplay("*Priority")]
        public int Priority { get; set; }

        [LocalizedDisplay("*From")]
        public string From { get; set; }

        [LocalizedDisplay("*To")]
        public string To { get; set; }

        [LocalizedDisplay("*CC")]
        public string CC { get; set; }

        [LocalizedDisplay("*Bcc")]
        public string Bcc { get; set; }

        [LocalizedDisplay("*Subject")]
        public string Subject { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*Body")]
        public string Body { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("*SentTries")]
        public int SentTries { get; set; }

        [LocalizedDisplay("*SentOn")]
        public DateTime? SentOn { get; set; }

        [LocalizedDisplay("*EmailAccountName")]
        public string EmailAccountName { get; set; }

        [LocalizedDisplay("*SendManually")]
        public bool SendManually { get; set; }

        [LocalizedDisplay("*Attachments")]
        public int AttachmentsCount { get; set; }

        public string ViewUrl { get; set; }

        [LocalizedDisplay("*Attachments")]
        public ICollection<QueuedEmailAttachmentModel> Attachments { get; set; } = new List<QueuedEmailAttachmentModel>();

        public class QueuedEmailAttachmentModel : EntityModelBase
        {
            public string Name { get; set; }
            public string MimeType { get; set; }
        }
    }

    public partial class QueuedEmailValidator : AbstractValidator<QueuedEmailModel>
    {
        public QueuedEmailValidator()
        {
            RuleFor(x => x.Priority).InclusiveBetween(0, 99999);
            RuleFor(x => x.From).NotEmpty();
            RuleFor(x => x.To).NotEmpty();
            RuleFor(x => x.SentTries).InclusiveBetween(0, 99999);
        }
    }

    internal class QueuedEmailMapper : Mapper<QueuedEmail, QueuedEmailModel>
    {
        private readonly SmartDbContext _db;
        private readonly IDateTimeHelper _dateTimeHelper;

        public QueuedEmailMapper(SmartDbContext db, IDateTimeHelper dateTimeHelper)
        {
            _db = db;
            _dateTimeHelper = dateTimeHelper;
        }

        protected override void Map(QueuedEmail from, QueuedEmailModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(QueuedEmail from, QueuedEmailModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            MiniMapper.Map(from, to);

            await _db.LoadReferenceAsync(from, x => x.EmailAccount);
            await _db.LoadCollectionAsync(from, x => x.Attachments);

            to.EmailAccountName = from.EmailAccount?.FriendlyName ?? string.Empty;
            to.AttachmentsCount = from.Attachments?.Count ?? 0;
            to.Attachments = from.Attachments
                .Select(x => new QueuedEmailModel.QueuedEmailAttachmentModel { Id = x.Id, Name = x.Name, MimeType = x.MimeType })
                .ToList();

            to.CreatedOn = _dateTimeHelper.ConvertToUserTime(from.CreatedOnUtc, DateTimeKind.Utc);
            if (from.SentOnUtc.HasValue)
            {
                to.SentOn = _dateTimeHelper.ConvertToUserTime(from.SentOnUtc.Value, DateTimeKind.Utc);
            }
        }
    }
}