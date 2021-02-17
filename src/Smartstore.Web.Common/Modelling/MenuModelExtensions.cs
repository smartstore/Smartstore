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
        /// <param name="context">Controller context to resolve current node. Can be <c>null</c>.</param>
        /// <returns>Menu model.</returns>
        public static async Task<MenuModel> CreateModelAsync(this IMenu menu, string template, ControllerContext context)
        {
            Guard.NotNull(menu, nameof(menu));

            var model = new MenuModel
            {
                Name = menu.Name,
                Template = template ?? menu.Name,
                Root = menu.Root,
                SelectedNode = await menu.ResolveCurrentNodeAsync(context)
            };

            await menu.ResolveElementCountAsync(model.SelectedNode, false);

            return model;
        }
    }
}
