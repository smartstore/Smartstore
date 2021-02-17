using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Menus;

namespace Smartstore.Web.Modelling
{
    public static class MenuModelExtensions
    {
        /// <summary>
        /// Creates a menu model.
        /// </summary>
        /// <param name="menu">Menu.</param>
        /// <param name="actionContext">Controller context to resolve current node. Can be <c>null</c>.</param>
        /// <returns>Menu model.</returns>
        public static async Task<MenuModel> CreateModelAsync(this IMenu menu, string template, ActionContext actionContext)
        {
            Guard.NotNull(menu, nameof(menu));

            var model = new MenuModel
            {
                Name = menu.Name,
                Template = template ?? menu.Name,
                Root = await menu.GetRootNodeAsync(),
                SelectedNode = await menu.ResolveCurrentNodeAsync(actionContext)
            };

            await menu.ResolveElementCountAsync(model.SelectedNode, false);

            return model;
        }
    }
}
