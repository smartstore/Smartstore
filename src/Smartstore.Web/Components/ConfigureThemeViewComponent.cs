using Smartstore.Core.Stores;
using Smartstore.Core.Theming;

namespace Smartstore.Web.Components
{
    public class ConfigureThemeViewComponent : SmartViewComponent
    {
        private readonly IThemeVariableService _themeVarService;
        private readonly IThemeContext _themeContext;
        private readonly IStoreContext _storeContext;

        public ConfigureThemeViewComponent(IThemeVariableService themeVarService, IThemeContext themeContext, IStoreContext storeContext)
        {
            _themeVarService = themeVarService;
            _themeContext = themeContext;
            _storeContext = storeContext;
        }

        public async Task<IViewComponentResult> InvokeAsync(string theme, int storeId)
        {
            if (theme.HasValue())
            {
                _themeContext.SetRequestTheme(theme);
            }

            if (storeId > 0)
            {
                _storeContext.SetRequestStore(storeId);
            }

            var model = TempData["OverriddenThemeVars"] ?? await _themeVarService.GetThemeVariablesAsync(theme, storeId);

            return View(model);
        }
    }
}
