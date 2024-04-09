using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using FluentValidation;
using Smartstore.Collections;

namespace Smartstore.Admin.Models.Messages
{
    [LocalizedDisplay("Admin.ContentManagement.MessageTemplates.Fields.")]
    public class MessageTemplateModel : TabbableModel, ILocalizedModel<MessageTemplateLocalizedModel>
    {
        [LocalizedDisplay("*AllowedTokens")]
        [IgnoreDataMember]
        public TreeNode<string> TokensTree { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.System.QueuedEmails.Fields.To")]
        public string To { get; set; }

        [LocalizedDisplay("Admin.System.QueuedEmails.Fields.ReplyTo")]
        public string ReplyTo { get; set; }

        [LocalizedDisplay("*AllowedTokens")]
        [IgnoreDataMember]
        public string LastModelTree { get; set; }

        [LocalizedDisplay("*BccEmailAddresses")]
        public string BccEmailAddresses { get; set; }

        [LocalizedDisplay("*Subject")]
        public string Subject { get; set; }

        [LocalizedDisplay("*Body")]
        [UIHint("Liquid")]
        public string Body { get; set; }

        [LocalizedDisplay("Common.Active")]
        public bool IsActive { get; set; }

        [LocalizedDisplay("*EmailAccount")]
        public int EmailAccountId { get; set; }

        [LocalizedDisplay("*SendManually")]
        public bool SendManually { get; set; }

        [LocalizedDisplay("*Attachment1FileId")]
        [UIHint("Media")]
        [AdditionalMetadata("album", "content")]
        [AdditionalMetadata("path", "message")]
        [AdditionalMetadata("typeFilter", "*")]
        [AdditionalMetadata("entityType", "MessageTemplate")]
        public int? Attachment1FileId { get; set; }

        [LocalizedDisplay("*Attachment2FileId")]
        [UIHint("Media")]
        [AdditionalMetadata("album", "content")]
        [AdditionalMetadata("path", "message")]
        [AdditionalMetadata("typeFilter", "*")]
        [AdditionalMetadata("entityType", "MessageTemplate")]
        public int? Attachment2FileId { get; set; }

        [LocalizedDisplay("*Attachment3FileId")]
        [UIHint("Media")]
        [AdditionalMetadata("album", "content")]
        [AdditionalMetadata("path", "message")]
        [AdditionalMetadata("typeFilter", "*")]
        [AdditionalMetadata("entityType", "MessageTemplate")]
        public int? Attachment3FileId { get; set; }

        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        public string EditUrl { get; set; }

        public List<MessageTemplateLocalizedModel> Locales { get; set; } = new();
    }

    [LocalizedDisplay("Admin.ContentManagement.MessageTemplates.Fields.")]
    public class MessageTemplateLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.System.QueuedEmails.Fields.To")]
        public string To { get; set; }

        [LocalizedDisplay("Admin.System.QueuedEmails.Fields.ReplyTo")]
        public string ReplyTo { get; set; }

        [LocalizedDisplay("*BccEmailAddresses")]
        public string BccEmailAddresses { get; set; }

        [LocalizedDisplay("*Subject")]
        public string Subject { get; set; }

        [LocalizedDisplay("*Body")]
        [UIHint("Liquid")]
        public string Body { get; set; }

        [LocalizedDisplay("*EmailAccount")]
        public int EmailAccountId { get; set; }

        [LocalizedDisplay("*Attachment1FileId")]
        [UIHint("Media")]
        [AdditionalMetadata("album", "content")]
        [AdditionalMetadata("path", "message")]
        [AdditionalMetadata("typeFilter", "*")]
        [AdditionalMetadata("entityType", "MessageTemplate")]
        public int? Attachment1FileId { get; set; }

        [LocalizedDisplay("*Attachment2FileId")]
        [UIHint("Media")]
        [AdditionalMetadata("album", "content")]
        [AdditionalMetadata("path", "message")]
        [AdditionalMetadata("typeFilter", "*")]
        [AdditionalMetadata("entityType", "MessageTemplate")]
        public int? Attachment2FileId { get; set; }

        [LocalizedDisplay("*Attachment3FileId")]
        [UIHint("Media")]
        [AdditionalMetadata("album", "content")]
        [AdditionalMetadata("path", "message")]
        [AdditionalMetadata("typeFilter", "*")]
        [AdditionalMetadata("entityType", "MessageTemplate")]
        public int? Attachment3FileId { get; set; }
    }

    public partial class MessageTemplateValidator : AbstractValidator<MessageTemplateModel>
    {
        public MessageTemplateValidator()
        {
            RuleFor(x => x.Subject).NotEmpty();
            RuleFor(x => x.Body).NotEmpty();
        }
    }
}
