using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;

namespace Smartstore.Core.Content.Blocks
{
    public abstract class BlockHandlerBase<T> : IBlockHandler<T> where T : IBlock
    {
        public ICommonServices Services { get; set; }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public ILocalizedEntityService LocalizedEntityService { get; set; }

		public virtual T Create(IBlockEntity entity)
		{
			return Activator.CreateInstance<T>();
		}

		public virtual T Load(IBlockEntity entity, StoryViewMode viewMode)
		{
			Guard.NotNull(entity, nameof(entity));

			var block = Create(entity);
			var json = entity.Model;

			if (json.IsEmpty())
			{
				return block;
			}

			JsonConvert.PopulateObject(json, block);

			if (block is IBindableBlock bindableBlock)
			{
				bindableBlock.BindEntityName = entity.BindEntityName;
				bindableBlock.BindEntityId = entity.BindEntityId;
			}

			return block;
		}

		public virtual bool IsValid(T block)
		{
			return true;
		}

		public virtual void Save(T block, IBlockEntity entity)
		{
			Guard.NotNull(entity, nameof(entity));

			if (block == null)
			{
				return;
			}

			var settings = new JsonSerializerSettings
			{
				ObjectCreationHandling = ObjectCreationHandling.Replace
			};

			entity.Model = JsonConvert.SerializeObject(block, Formatting.None, settings);

			// save BindEntintyName & BindEntintyId
			if (block is IBindableBlock bindableBlock)
			{
				entity.BindEntityId = bindableBlock.BindEntityId;
				entity.BindEntityName = bindableBlock.BindEntityName;
			}
		}

		public virtual Task AfterSaveAsync(IBlockContainer container, IBlockEntity entity)
		{
			// Default impl does nothing.
			return Task.CompletedTask;
		}

		public virtual void BeforeRender(IBlockContainer container, StoryViewMode viewMode, IBlockHtmlParts htmlParts)
		{
			// Default impl does nothing.
		}

		public virtual Task<string> CloneAsync(IBlockEntity sourceEntity, IBlockEntity clonedEntity)
		{
			return Task.FromResult(sourceEntity.Model);
		}

		public Task RenderAsync(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper)
		{
			return RenderCoreAsync(element, templates, htmlHelper, htmlHelper.ViewContext.Writer);
		}

		public async Task<IHtmlContent> ToHtmlContentAsync(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper)
		{
            using var writer = new StringWriter(CultureInfo.CurrentCulture);
            await RenderCoreAsync(element, templates, htmlHelper, writer);
            return new HtmlString(writer.ToString());
        }

		protected virtual Task RenderCoreAsync(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper, TextWriter textWriter)
		{
			return RenderByViewAsync(element, templates, htmlHelper, textWriter);
		}

        protected Task RenderByViewAsync(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper, TextWriter textWriter)
        {
            Guard.NotNull(element, nameof(element));
            Guard.NotNull(templates, nameof(templates));
            Guard.NotNull(htmlHelper, nameof(htmlHelper));
            Guard.NotNull(textWriter, nameof(textWriter));

            var actionContext = GetActionContextFor(element, htmlHelper.ViewContext);
            var viewResult = FindFirstView(element.Metadata, templates, actionContext, out var searchedLocations);

            if (viewResult == null)
            {
                var msg = string.Format("No template found for '{0}'. Searched locations:\n{1}.", string.Join(", ", templates), string.Join("\n", searchedLocations));
                Logger.Debug(msg);
                throw new FileNotFoundException(msg);
            }

			var mvcViewOptions = actionContext.HttpContext.RequestServices.GetRequiredService<IOptions<MvcViewOptions>>();
			var modelMetadataProvider = actionContext.HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
			var viewData = new ViewDataDictionary(modelMetadataProvider, htmlHelper.ViewContext.ModelState)
			{
				Model = element.Block
			};
            viewData.TemplateInfo.HtmlFieldPrefix = "Block";

            var viewContext = new ViewContext(
                htmlHelper.ViewContext,
                viewResult.View,
                viewData,
                htmlHelper.ViewContext.TempData,
                textWriter,
				mvcViewOptions.Value.HtmlHelperOptions);

			// TODO: (core) Use IRazorViewInvoker?
			return viewResult.View.RenderAsync(viewContext);
        }

		protected virtual async Task RenderByWidgetAsync(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper, TextWriter textWriter)
        {
			Guard.NotNull(element, nameof(element));
			Guard.NotNull(templates, nameof(templates));
			Guard.NotNull(htmlHelper, nameof(htmlHelper));
			Guard.NotNull(textWriter, nameof(textWriter));

			var widget = templates.Select(x => GetWidget(element, x)).FirstOrDefault(x => x != null);
			if (widget == null)
			{
				throw new InvalidOperationException("The return value of the 'GetWidget()' method cannot be NULL.");
			}

			// TODO: (core) Implement BlockHandlerBase.RenderByWidgetAsync() properly
			await widget.InvokeAsync(htmlHelper.ViewContext);
		}

		protected virtual WidgetInvoker GetWidget(IBlockContainer element, string template)
		{
			throw new NotImplementedException();
		}

		private static ActionContext GetActionContextFor(IBlockContainer element, ActionContext originalContext)
		{
			if (element.Metadata.IsInbuilt)
			{
				return originalContext;
			}

			// Change "area" token in RouteData in order to begin search in the module's view folder.
			var originalRouteData = originalContext.RouteData;
			var routeData = new RouteData(originalRouteData);
			routeData.Values.Merge(originalRouteData.Values);
			routeData.DataTokens["area"] = element.Metadata.AreaName; // TODO: (core) Check whether DataTokens["area"] is still the right place for Area replacement in AspNetCore.

			return new ActionContext
			{
				RouteData = routeData,
				HttpContext = originalContext.HttpContext,
				ActionDescriptor = originalContext.ActionDescriptor
			};
		}

		private static ViewEngineResult FindFirstView(IBlockMetadata blockMetadata, IEnumerable<string> templates, ActionContext actionContext, out ICollection<string> searchedLocations)
		{
			searchedLocations = new List<string>();

			var viewEngine = actionContext.HttpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
			foreach (var template in templates)
			{
				var viewName = string.Concat("BlockTemplates/", blockMetadata.SystemName, "/", template);
				var viewResult = viewEngine.FindView(actionContext, viewName, false);
				searchedLocations.AddRange(viewResult.SearchedLocations);
				if (viewResult.View != null)
				{
					return viewResult;
				}
			}

			return null;
		}
	}
}
