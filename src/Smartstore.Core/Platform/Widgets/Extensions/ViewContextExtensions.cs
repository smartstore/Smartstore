using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Smartstore.Core.Widgets
{
    public static class ViewContextExtensions
    {
        /// <summary>
        /// Clones a <see cref="ViewContext"/> instance.
        /// </summary>
        /// <returns>The cloned view context</returns>
        public static ViewContext Clone(this ViewContext viewContext)
        {
            return Clone(viewContext, viewContext.ViewData);
        }

        /// <summary>
        /// Clones a <see cref="ViewContext"/> instance and modifies the model.
        /// </summary>
        /// <param name="model">The new model instance to use instead of the original one.</param>
        /// <returns>The cloned view context</returns>
        public static ViewContext Clone<TModel>(this ViewContext viewContext, TModel model)
        {
            Guard.NotNull(model, nameof(model));

            return Clone(viewContext, (ViewDataDictionary)new ViewDataDictionary<TModel>(viewContext.ViewData, model));
        }

        /// <summary>
        /// Clones a <see cref="ViewContext"/> instance and modifies the original <see cref="ViewDataDictionary"/> instance.
        /// </summary>
        /// <param name="viewData">The new view data instance to use instead of the original one.</param>
        /// <returns>The cloned view context</returns>
        public static ViewContext Clone(this ViewContext viewContext, ViewDataDictionary viewData)
        {
            Guard.NotNull(viewContext, nameof(viewContext));
            Guard.NotNull(viewData, nameof(viewData));

            return new ViewContext(
                viewContext,
                viewContext.View,
                viewData,
                viewContext.Writer);
        }
    }
}
