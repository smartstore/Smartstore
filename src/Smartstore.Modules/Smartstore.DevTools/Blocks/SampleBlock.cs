using System.Runtime.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Content.Blocks;

namespace Smartstore.DevTools.Blocks
{
    /// <summary>
    /// The block handler is the controller which is responsible for loading, instantiating, storing and rendering block types.
    /// The <see cref="BlockHandlerBase{T}"/> abstract class already implements important parts.
    /// You can, however, overwrite any method to fulfill your custom needs.
    /// </summary>
    [Block("sample", Icon = "far fa-terminal", FriendlyName = "Sample", DisplayOrder = 50, IsInternal = true)] // REMOVE IsInternal = true to display the block in the page builder
    public class SampleBlockHandler : BlockHandlerBase<SampleBlock>
    {
        public override Task<SampleBlock> LoadAsync(IBlockEntity entity, StoryViewMode viewMode)
        {
            var block = base.Load(entity, viewMode);

            // By default 'BlockHandlerBase<TBlock>' stores block instance data as JSON in the 'Model' field of the 'PageStoryBlock' table.
            // You can, however, store data anywhere you like and override loading behaviour in this method.
            if (viewMode == StoryViewMode.Edit)
            {
                // You can prepare your model especially for the edit mode of the block, 
                // e.g. add some SelectListItems for dropdownmenus which you will only need in the edit mode of the block
                block.MyProperties.Add(new SelectListItem { Text = "Item1", Value = "1" });
            }
            else if (viewMode == StoryViewMode.GridEdit)
            {
                // Manipulate properties especially for the grid edit mode e.g. turn of any animation which could distract the user from editing the grid. 
                //block.Autoplay = false;
            }

            return Task.FromResult(block);
        }

        public override async Task SaveAsync(SampleBlock block, IBlockEntity entity)
        {
            // By default 'BlockHandlerBase<TBlock>' stores block instance data as JSON in the 'Model' field of the 'PageStoryBlock' table.
            // You can, however, store data anywhere you like and override persistance behaviour in this method.

            await base.SaveAsync(block, entity);
        }

        /// <summary>
        /// By default block templates (Edit & Public) will be searched in '{Module}\Views\Shared\BlockTemplates\{BlockSystemName}',
        /// while {Module} represents your custom module's system name.
        /// The public action can address a deviating route by overwriting RenderCoreAsync & GetWidget.
        /// You can override this behaviour by e.g. calling a widget in your module controller instead of directly rendering a view.
        /// For this to take effect you have to override both methods 'RenderCoreAsync()' and 'GetWidget()'
        /// </summary>
        //protected override async Task RenderCoreAsync(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper, TextWriter textWriter)
        //{
        //    if (templates.First() == "Edit")
        //    {
        //        await base.RenderCoreAsync(element, templates, htmlHelper, textWriter);
        //    }
        //    else
        //    {
        //        await base.RenderByWidgetAsync(element, templates, htmlHelper, textWriter);
        //    }
        //}

        //protected override WidgetInvoker GetWidget(IBlockContainer element, IHtmlHelper htmlHelper, string template)
        //{
        //    var block = (SampleBlock)element.Block;
        //    return new ComponentWidgetInvoker(typeof(HomeBestSellersViewComponent), new
        //    {
        //        productThumbPictureSize = 512
        //    });
        //}
    }

    /// <summary>
    /// Any type that implements the 'IBlock' interface acts as a model.
    /// </summary>
    public class SampleBlock : IBlock
    {
        [LocalizedDisplay("Plugins.SmartStore.DevTools.Block.Property")]
        public string Property { get; set; }

        // By default a block instance will be converted to JSON and stored in the 'Model' field of the 'PageStoryBlock' table.
        // If your block type contains some special properties - e.g. volatile data for the edit mode - and you don't want them to be persisted, add the [IgnoreDataMember] attribute to your property.
        [IgnoreDataMember]
        public List<SelectListItem> MyProperties { get; set; } = new();
    }

    /// <summary>
    /// This is the validator which is used to validate the user input while editing your block
    /// </summary>
    public partial class SampleBlockValidator : AbstractValidator<SampleBlock>
    {
        public SampleBlockValidator()
        {
            RuleFor(x => x.Property).NotEmpty();
        }
    }
}
