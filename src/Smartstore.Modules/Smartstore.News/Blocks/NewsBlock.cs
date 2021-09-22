using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Smartstore.Core.Content.Blocks;
using Smartstore.Web.Modelling;

namespace Smartstore.News.Blocks
{
    // TODO: (mh) (core) Do this correctly when its time to.
    //[Block("news", Icon = "far fa-newspaper", FriendlyName = "News", DisplayOrder = 200)]
    //public class NewsBlockHandler : DefaultBlockHandler<NewsBlock>
    //{
    //    public override NewsBlock Load(IBlockEntity entity, StoryViewMode viewMode)
    //    {
    //        var block = base.Load(entity, viewMode);
    //        return block;
    //    }

    //    public override void Save(NewsBlock block, IBlockEntity entity)
    //    {
    //        base.Save(block, entity);
    //    }

    //    protected override void RenderCore(IBlockContainer element, IEnumerable<string> templates, HtmlHelper htmlHelper, TextWriter textWriter)
    //    {
    //        if (templates.First() == "Edit")
    //        {
    //            base.RenderCore(element, templates, htmlHelper, textWriter);
    //        }
    //        else
    //        {
    //            RenderByChildAction(element, templates, htmlHelper, textWriter);
    //        }
    //    }

    //    //protected override RouteInfo GetRoute(IBlockContainer element, string template)
    //    //{
    //    //    var block = (NewsBlock)element.Block;
    //    //    return new RouteInfo("NewsSummary", "News", new
    //    //    {
    //    //        maxPostAmount = block.MaxPostAmount,
    //    //        maxAgeInDays = block.MaxAgeInDays,
    //    //        renderHeading = block.RenderHeading,
    //    //        newsHeading = element.Title,
    //    //        disableCommentCount = block.DisableCommentCount,
    //    //        area = ""
    //    //    });
    //    //}
    //}

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
