using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Content.Blocks;
using Smartstore.Core.Widgets;
using Smartstore.Blog.Components;
using Smartstore.Web.Modelling;
using Newtonsoft.Json;

namespace Smartstore.Blog.Blocks
{
    [Block("blog", Icon = "fa fa-blog", FriendlyName = "Blog", DisplayOrder = 150)]
    public class BlogBlockHandler : BlockHandlerBase<BlogBlock>
    {
        public override Task<BlogBlock> LoadAsync(IBlockEntity entity, StoryViewMode viewMode)
        {
            var block = base.LoadAsync(entity, viewMode);
            return block;
        }

        public override Task SaveAsync(BlogBlock block, IBlockEntity entity)
        {
            return base.SaveAsync(block, entity);
        }

        protected override async Task RenderCoreAsync(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper, TextWriter textWriter)
        {
            if (templates.First() == "Edit")
            {
                await base.RenderCoreAsync(element, templates, htmlHelper, textWriter);
            }
            else
            {
                await RenderByWidgetAsync(element, templates, htmlHelper, textWriter);
            }
        }

        protected override WidgetInvoker GetWidget(IBlockContainer element, string template)
        {
            var block = (BlogBlock)element.Block;

            return new ComponentWidgetInvoker(typeof(BlogSummaryViewComponent), new
            {
                maxPostAmount = block.MaxPostAmount,
                maxAgeInDays = block.MaxAgeInDays,
                renderHeading = block.RenderHeading,
                blogHeading = element.Title,
                disableCommentCount = block.DisableCommentCount,
                postsWithTag = block.PostsWithTag
            }) { };
        }
    }

    [LocalizedDisplay("Plugins.SmartStore.PageBuilder.Blog.")]
    public class BlogBlock : IBlock
    {
        [LocalizedDisplay("*RenderHeading")]
        public bool RenderHeading { get; set; }

        [LocalizedDisplay("*DisableCommentCount")]
        public bool DisableCommentCount { get; set; }

        [LocalizedDisplay("*MaxPostAmount")]
        public int? MaxPostAmount { get; set; }

        [LocalizedDisplay("*MaxAgeInDays")]
        public int? MaxAgeInDays { get; set; }

        [LocalizedDisplay("PostsWithTag")]
        public string PostsWithTag { get; set; }

        [JsonIgnore]
        public IList<SelectListItem> AvailableTags { get; set; } = new List<SelectListItem>();
    }

    public partial class BlogBlockValidator : AbstractValidator<BlogBlock>
    {
        public BlogBlockValidator()
        {
            RuleFor(x => x.MaxPostAmount).GreaterThanOrEqualTo(1).When(x => x.MaxPostAmount.HasValue);
            RuleFor(x => x.MaxAgeInDays).GreaterThanOrEqualTo(1).When(x => x.MaxAgeInDays.HasValue);
        }
    }
}
