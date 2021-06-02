using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Messaging;
using Smartstore.Web.Modelling;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Smartstore.Admin.Models.Messages
{
    [LocalizedDisplay("Admin.System.QueuedEmails.Fields.")]
    public class QueuedEmailModel : EntityModelBase
    {
        [LocalizedDisplay("Admin.Common.Entity.Fields.Id")]
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

        // TODO: (ms) (core) Where's the resource for this?
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

    public class QueuedEmailMapper : Mapper<QueuedEmail, QueuedEmailModel>
    {
        protected override void Map(QueuedEmail from, QueuedEmailModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
                        
            to.EmailAccountName = from.EmailAccount?.FriendlyName ?? string.Empty;
            to.AttachmentsCount = from.Attachments?.Count ?? 0;
            to.Attachments = from.Attachments
                .Select(x => new QueuedEmailModel.QueuedEmailAttachmentModel { Id = x.Id, Name = x.Name, MimeType = x.MimeType })
                .ToList();
        }
    }
}