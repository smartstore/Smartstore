using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Collections;
using Smartstore.Core.Messaging;

namespace Smartstore.Admin.Models.Messages
{
    [LocalizedDisplay("Admin.Promotions.Campaigns.Fields.")]
    public class CampaignModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Subject")]
        public string Subject { get; set; }

        [LocalizedDisplay("*Body")]
        [UIHint("Html")]
        [AdditionalMetadata("lazy", false)]
        public string Body { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("*AllowedTokens")]
        [UIHint("ModelTree")]
        public TreeNode<ModelTreeMember> LastModelTree { get; set; }

        [LocalizedDisplay("*TestEmail")]
        public string TestEmail { get; set; }

        [UIHint("CustomerRoles")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
        public int[] SelectedCustomerRoleIds { get; set; }

        [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
        public bool SubjectToAcl { get; set; }

        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        public string EditUrl { get; set; }
    }

    public partial class CampaignValidator : AbstractValidator<CampaignModel>
    {
        public CampaignValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Subject).NotEmpty();
            RuleFor(x => x.Body).NotEmpty();
        }
    }
}
