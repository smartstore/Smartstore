using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Content.Blocks;
using Smartstore.Core.Widgets;
using Smartstore.News.Components;
using Smartstore.Web.Modelling;

namespace Smartstore.News.Blocks
{
    [Block("news", Icon = "far fa-newspaper", FriendlyName = "News", DisplayOrder = 200)]
    public class NewsBlockHandler : BlockHandlerBase<NewsBlock>
    {
        public override NewsBlock Load(IBlockEntity entity, StoryViewMode viewMode)
        {
            var block = base.Load(entity, viewMode);
            return block;
        }

        public override void Save(NewsBlock block, IBlockEntity entity)
        {
            base.Save(block, entity);
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
            var block = (NewsBlock)element.Block;

            return new ComponentWidgetInvoker(typeof(NewsSummaryViewComponent), new
            {
                maxPostAmount = block.MaxPostAmount,
                maxAgeInDays = block.MaxAgeInDays,
                renderHeading = block.RenderHeading,
                newsHeading = element.Title,
                disableCommentCount = block.DisableCommentCount,
            }) { };
        }
    }

    [LocalizedDisplay("Plugins.SmartStore.PageBuilder.Blog.")]
    public class NewsBlock : IBlock
    {
        [LocalizedDisplay("*RenderHeading")]
        public bool RenderHeading { get; set; }

        [LocalizedDisplay("*DisableCommentCount")]
        public bool DisableCommentCount { get; set; }

        [LocalizedDisplay("*MaxPostAmount")]
        public int? MaxPostAmount { get; set; }

        [LocalizedDisplay("*MaxAgeInDays")]
        public int? MaxAgeInDays { get; set; }
    }

    public partial class NewsBlockValidator : AbstractValidator<NewsBlock>
    {
        public NewsBlockValidator()
        {
            RuleFor(x => x.MaxPostAmount).GreaterThanOrEqualTo(1).When(x => x.MaxPostAmount.HasValue);
            RuleFor(x => x.MaxAgeInDays).GreaterThanOrEqualTo(1).When(x => x.MaxAgeInDays.HasValue);
        }
    }
}
