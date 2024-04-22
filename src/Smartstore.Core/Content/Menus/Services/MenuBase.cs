using Microsoft.AspNetCore.Mvc;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Localization;
using Smartstore.Diagnostics;

namespace Smartstore.Core.Content.Menus
{
    public abstract class MenuBase : IMenu
    {
        /// <summary>
        /// Key for Menu caching
        /// </summary>
        /// <remarks>
        /// {0} : Menu name
        /// {1} : Menu specific key suffix
        /// </remarks>
        internal const string MENU_KEY = "pres:menu:{0}-{1}";
        internal const string MENU_PATTERN_KEY = "pres:menu:{0}*";

        private TreeNode<MenuItem> _currentNode;
        private bool _currentNodeResolved;
        private List<string> _providers;

        public ICommonServices Services { get; set; }
        public Localizer T { get; set; } = NullLocalizer.Instance;
        public required IMenuPublisher MenuPublisher { protected get; set; }

        public abstract string Name { get; }

        public virtual bool ApplyPermissions => true;

        public virtual async Task<TreeNode<MenuItem>> GetRootNodeAsync()
        {
            var cacheKey = MENU_KEY.FormatInvariant(Name, GetCacheKey());

            var rootNode = await Services.Cache.GetAsync(cacheKey, async (o) =>
            {
                using (Services.Chronometer.Step($"Build menu '{Name}'"))
                {
                    var root = await BuildAsync(o);

                    MenuPublisher.RegisterMenus(root, Name);

                    if (ApplyPermissions)
                    {
                        await DoApplyPermissionsAsync(root);
                    }

                    await OnMenuBuilt(root);
                    await Services.EventPublisher.PublishAsync(new MenuBuiltEvent(Name, root));

                    return root;
                }
            });

            return rootNode;
        }

        protected virtual Task OnMenuBuilt(TreeNode<MenuItem> root)
        {
            // for integrators
            return Task.CompletedTask;
        }

        protected virtual async Task DoApplyPermissionsAsync(TreeNode<MenuItem> root)
        {
            // Hide based on permissions
            await root.TraverseAwait(async x =>
            {
                if (!await MenuItemAccessPermittedAsync(x.Value))
                {
                    x.Value.Visible = false;
                }
            });

            // Hide dropdown nodes when no child is visible
            root.Traverse(x =>
            {
                var item = x.Value;
                if (!item.IsGroupHeader && !item.HasRoute)
                {
                    if (!x.Children.Any(child => child.Value.Visible))
                    {
                        item.Visible = false;
                    }
                }
            });
        }

        protected abstract string GetCacheKey();

        protected abstract Task<TreeNode<MenuItem>> BuildAsync(CacheEntryOptions cacheEntryOptions);

        public virtual Task ResolveElementCountAsync(TreeNode<MenuItem> curNode, bool deep = false)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<TreeNode<MenuItem>> ResolveCurrentNodeAsync(ActionContext actionContext)
        {
            if (!_currentNodeResolved)
            {
                _currentNode = (await GetRootNodeAsync()).SelectNode(x => x.Value.IsCurrent(actionContext), true);
                _currentNodeResolved = true;
            }

            return _currentNode;
        }

        public IDictionary<string, TreeNode<MenuItem>> GetAllCachedMenus()
        {
            var cache = Services.Cache;
            var keys = cache.Keys(MENU_PATTERN_KEY.FormatInvariant(Name));

            var trees = new Dictionary<string, TreeNode<MenuItem>>(keys.Count());

            foreach (var key in keys)
            {
                var tree = cache.Get<TreeNode<MenuItem>>(key);
                if (tree != null)
                {
                    trees[key] = tree;
                }
            }

            return trees;
        }

        public Task ClearCacheAsync()
        {
            return Services.Cache.RemoveByPatternAsync(MENU_PATTERN_KEY.FormatInvariant(Name));
        }

        #region Utilities

        protected virtual async Task<bool> ContainsProviderAsync(string provider)
        {
            Guard.NotEmpty(provider, nameof(provider));

            if (_providers == null)
            {
                _providers = (await GetRootNodeAsync()).GetMetadata<List<string>>("Providers") ?? new List<string>();
            }

            return _providers.Contains(provider);
        }

        protected virtual T GetRequestValue<T>(ActionContext actionContext, string name)
        {
            Guard.NotNull(actionContext, nameof(actionContext));
            Guard.NotEmpty(name, nameof(name));

            var httpContext = actionContext.HttpContext;

            if (!httpContext.TryGetRouteValueAs(name, out T value))
            {
                httpContext.Request.TryGetValueAs(name, out value);
            }

            return value;
        }

        private async Task<bool> MenuItemAccessPermittedAsync(MenuItem item)
        {
            var result = true;

            if (item.PermissionNames.HasValue())
            {
                var permitted = await item
                    .PermissionNames
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .AnyAsync(x => Services.Permissions.AuthorizeAsync(x.Trim(), allowByDescendantPermission: true));

                if (!permitted)
                {
                    result = false;
                }
            }

            return result;
        }

        #endregion
    }
}
