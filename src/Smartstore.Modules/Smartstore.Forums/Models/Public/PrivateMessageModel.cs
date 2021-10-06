using System;
using System.Collections.Generic;
using FluentValidation;
using Smartstore.Collections;
using Smartstore.Web.Modelling;

namespace Smartstore.Forums.Models.Public
{
    public partial class PrivateMessageListModel : PagedListBase
    {
        public PrivateMessageListModel(IPageable pageable) 
            : base(pageable)
        {
        }

        public List<PrivateMessageModel> Messages { get; set; }
    }

    public partial class PrivateMessageModel : EntityModelBase
    {
        public int FromCustomerId { get; set; }
        public string CustomerFromName { get; set; }
        public bool AllowViewingFromProfile { get; set; }

        public int ToCustomerId { get; set; }
        public string CustomerToName { get; set; }
        public bool AllowViewingToProfile { get; set; }

        public string Subject { get; set; }
        public string Message { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsRead { get; set; }
    }


    public class PrivateMessageValidator : AbstractValidator<PrivateMessageModel>
    {
        public PrivateMessageValidator()
        {
            RuleFor(x => x.Subject).NotEmpty();
            RuleFor(x => x.Message).NotEmpty();
        }
    }
}
