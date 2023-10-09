using Smartstore.Core.Content.Menus;

namespace Smartstore.Web.Rendering.Menus
{
    public static class IMenuExtensions
    {
        /// <summary>
        /// Creates a menu model.
        /// </summary>
        /// <param name="menu">Menu.</param>
        /// <param name="actionContext">Action context to resolve current node. Can be <c>null</c>.</param>
        /// <returns>Menu model.</returns>
        public static async Task<MenuModel> CreateModelAsync(this IMenu menu, string template, ActionContext actionContext)
        {
            Guard.NotNull(menu);

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
