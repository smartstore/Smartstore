using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;

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
				ContractResolver = SmartContractResolver.Instance,
				TypeNameHandling = TypeNameHandling.Objects,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				ObjectCreationHandling = ObjectCreationHandling.Replace,
				NullValueHandling = NullValueHandling.Ignore
			};

			entity.Model = JsonConvert.SerializeObject(block, Formatting.None, settings);

			// save BindEntintyName & BindEntintyId
			if (block is IBindableBlock bindableBlock)
			{
				entity.BindEntityId = bindableBlock.BindEntityId;
				entity.BindEntityName = bindableBlock.BindEntityName;
			}
		}

		public virtual void AfterSave(IBlockContainer container, IBlockEntity entity)
		{
			// Default impl does nothing.
		}

		public virtual void BeforeRender(IBlockContainer container, StoryViewMode viewMode, IBlockHtmlParts htmlParts)
		{
			// Default impl does nothing.
		}

		public virtual string Clone(IBlockEntity sourceEntity, IBlockEntity clonedEntity)
		{
			return sourceEntity.Model;
		}

		public void Render(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper)
		{
			RenderCore(element, templates, htmlHelper, htmlHelper.ViewContext.Writer);
			throw new NotImplementedException();
		}

		public IHtmlContent ToHtmlContent(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper)
		{
			using (var writer = new StringWriter(CultureInfo.CurrentCulture))
			{
				RenderCore(element, templates, htmlHelper, writer);
				return new HtmlString(writer.ToString());
			}
		}

		protected virtual void RenderCore(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper, TextWriter textWriter)
		{
			RenderByView(element, templates, htmlHelper, textWriter);
		}

        protected void RenderByView(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper, TextWriter textWriter)
        {
            Guard.NotNull(element, nameof(element));
            Guard.NotNull(templates, nameof(templates));
            Guard.NotNull(htmlHelper, nameof(htmlHelper));
            Guard.NotNull(textWriter, nameof(textWriter));

            var viewContext = htmlHelper.ViewContext;

            if (!element.Metadata.IsInbuilt)
            {
                // Change "area" token in RouteData in order to begin search in the plugin's view folder.
                var originalRouteData = htmlHelper.ViewContext.RouteData;
                var routeData = new RouteData(originalRouteData);
                routeData.Values.Merge(originalRouteData.Values);
                routeData.DataTokens["area"] = element.Metadata.AreaName;

                viewContext = new ViewContext
                {
                    RouteData = routeData,
                    HttpContext = htmlHelper.ViewContext.HttpContext
                };
            }

            var viewResult = FindFirstView(element.Metadata, templates, viewContext, out var searchedLocations);

            if (viewResult == null)
            {
                var msg = string.Format("No template found for '{0}'. Searched locations:\n{1}.", string.Join(", ", templates), string.Join("\n", searchedLocations));
                Logger.Debug(msg);
                throw new FileNotFoundException(msg);
            }

            // TODO: (mh) (core) Test this.
            var viewData = new ViewDataDictionary((ViewDataDictionary)element.Block);
            viewData.TemplateInfo.HtmlFieldPrefix = "Block";

            viewContext = new ViewContext(
                htmlHelper.ViewContext,
                viewResult.View,
                viewData,
                htmlHelper.ViewContext.TempData,
                textWriter,
                new HtmlHelperOptions());

            viewResult.View.RenderAsync(viewContext);
        }

		private ViewEngineResult FindFirstView(IBlockMetadata blockMetadata, IEnumerable<string> templates, ViewContext viewContext, out ICollection<string> searchedLocations)
		{
			searchedLocations = new List<string>();

			var viewEngine = Services.Resolve<IRazorViewEngine>();
			foreach (var template in templates)
			{
				var viewName = string.Concat("BlockTemplates/", blockMetadata.SystemName, "/", template);
				var viewResult = viewEngine.FindView(viewContext, viewName, false);
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
