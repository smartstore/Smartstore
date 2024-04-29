using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;

namespace Smartstore.Admin.Models.Topics
{
    [LocalizedDisplay("Admin.ContentManagement.Topics.Fields.")]
    public class TopicModel : TabbableModel, ILocalizedModel<TopicLocalizedModel>
    {
        public TopicModel()
        {
            AvailableTitleTags.AddRange(new[]
            {
                new SelectListItem { Text = "h1", Value = "h1" },
                new SelectListItem { Text = "h2", Value = "h2" },
                new SelectListItem { Text = "h3", Value = "h3" },
                new SelectListItem { Text = "h4", Value = "h4" },
                new SelectListItem { Text = "h5", Value = "h5" },
                new SelectListItem { Text = "h6", Value = "h6" },
                new SelectListItem { Text = "div", Value = "div" },
                new SelectListItem { Text = "span", Value = "span" }
            });
        }

        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        [UIHint("CustomerRoles")]
        [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
        public int[] SelectedCustomerRoleIds { get; set; }

        [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
        public bool SubjectToAcl { get; set; }

        [LocalizedDisplay("*CookieType")]
        public int? CookieType { get; set; }
        public List<SelectListItem> AvailableCookieTypes { get; set; } = new();

        [LocalizedDisplay("*SystemName")]
        public string SystemName { get; set; }

        [LocalizedDisplay("*HtmlId")]
        public string HtmlId { get; set; }

        [LocalizedDisplay("*BodyCssClass")]
        public string BodyCssClass { get; set; }

        [LocalizedDisplay("*IncludeInSitemap")]
        public bool IncludeInSitemap { get; set; }

        [LocalizedDisplay("*IsPasswordProtected")]
        public bool IsPasswordProtected { get; set; }

        [LocalizedDisplay("*Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [LocalizedDisplay("*URL")]
        public string Url { get; set; }

        [LocalizedDisplay("*ShortTitle")]
        public string ShortTitle { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        [LocalizedDisplay("*Intro")]
        public string Intro { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*Body")]
        public string Body { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        public string MetaDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        public string MetaTitle { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }

        [LocalizedDisplay("*RenderAsWidget")]
        public bool RenderAsWidget { get; set; }

        [LocalizedDisplay("*WidgetZone")]
        [UIHint("WidgetZone")]
        public string[] WidgetZone { get; set; }

        [LocalizedDisplay("*WidgetZone")]
        public string WidgetZoneValue { get; set; }

        [LocalizedDisplay("*WidgetWrapContent")]
        public bool WidgetWrapContent { get; set; } = true;

        [LocalizedDisplay("*WidgetShowTitle")]
        public bool WidgetShowTitle { get; set; }

        [LocalizedDisplay("*WidgetBordered")]
        public bool WidgetBordered { get; set; }

        [LocalizedDisplay("*Priority")]
        public int Priority { get; set; }

        [LocalizedDisplay("*TitleTag")]
        public string TitleTag { get; set; }

        [LocalizedDisplay("*IsSystemTopic")]
        public bool IsSystemTopic { get; set; }

        [LocalizedDisplay("Common.Published")]
        public bool IsPublished { get; set; }

        public List<SelectListItem> AvailableTitleTags { get; private set; } = new();

        public List<TopicLocalizedModel> Locales { get; set; } = new();

        [LocalizedDisplay("Admin.ContentManagement.MenuLinks")]
        public Dictionary<string, string> MenuLinks { get; set; } = new();

        public string ViewUrl { get; set; }
    }

    [LocalizedDisplay("Admin.ContentManagement.Topics.Fields.")]
    public class TopicLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*ShortTitle")]
        public string ShortTitle { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        [LocalizedDisplay("*Intro")]
        public string Intro { get; set; }

        [LocalizedDisplay("*Body")]
        [UIHint("Html")]
        [AdditionalMetadata("ForceRootBlock", false)]
        public string Body { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        public string MetaDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        public string MetaTitle { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }
    }

    public partial class TopicValidator : AbstractValidator<TopicModel>
    {
        public TopicValidator(Localizer T)
        {
            RuleFor(x => x.SystemName).NotEmpty();
            RuleFor(x => x.HtmlId)
                .Must(u => u.IsEmpty() || !u.Any(x => char.IsWhiteSpace(x)))
                .WithMessage(T("Admin.Common.HtmlId.NoWhiteSpace"));

            RuleFor(x => x.IsPasswordProtected)
                .Equal(false)
                .When(x => x.RenderAsWidget)
                .WithMessage(T("Admin.ContentManagement.Topics.Validation.NoPasswordAllowed"));

            RuleFor(x => x.Password)
                .NotEmpty()
                .When(x => x.IsPasswordProtected && !x.RenderAsWidget)
                .WithMessage(T("Admin.ContentManagement.Topics.Validation.NoEmptyPassword"));
        }
    }

    [Mapper(Lifetime = ServiceLifetime.Singleton)]
    public class TopicMapper : IMapper<Topic, TopicModel>
    {
        public async Task MapAsync(Topic from, TopicModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.SeName = await from.GetActiveSlugAsync(0, true, false);
            to.WidgetWrapContent = from.WidgetWrapContent ?? true;
        }
    }
}
