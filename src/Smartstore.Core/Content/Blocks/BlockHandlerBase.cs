using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
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
			// TODO: (mh) (core) Implement BlockHandlerBase.Render()
			//RenderCore(element, templates, htmlHelper, htmlHelper.ViewContext.Writer);
			throw new NotImplementedException();
		}

		public IHtmlContent ToHtmlContent(IBlockContainer element, IEnumerable<string> templates, IHtmlHelper htmlHelper)
		{
			// TODO: (mh) (core) Implement BlockHandlerBase.ToHtmlContent()
			using (var writer = new StringWriter(CultureInfo.CurrentCulture))
			{
				//RenderCore(element, templates, htmlHelper, writer);
				//return MvcHtmlString.Create(writer.ToString());
				throw new NotImplementedException();
			}
		}
	}
}
