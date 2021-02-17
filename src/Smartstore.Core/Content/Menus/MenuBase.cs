using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Smartstore.Collections;
using Smartstore.Diagnostics;
using Smartstore.Utilities;

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

        public IMenuPublisher MenuPublisher { get; set; }

        public abstract string Name { get; }

        public virtual bool ApplyPermissions => true;

        public TreeNode<MenuItem> Root
        {
            get
            {
                var cacheKey = MENU_KEY.FormatInvariant(Name, GetCacheKey());

                var rootNode = Services.Cache.GetAsync(cacheKey, async () =>
                {
                    using (Services.Chronometer.Step($"Build menu '{Name}'"))
                    {
                        var root = await BuildAsync();

                        MenuPublisher.RegisterMenus(root, Name);

                        if (ApplyPermissions)
                        {
                            DoApplyPermissions(root);
                        }

                        await Services.EventPublisher.PublishAsync(new MenuBuiltEvent(Name, root));

                        return root;
                    }
                });

                return rootNode.Result;
            }
        }

        protected virtual void DoApplyPermissions(TreeNode<MenuItem> root)
        {
            // Hide based on permissions
            root.Traverse(async x =>
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
                if (!item.IsGroupHeader && !item.HasRoute())
                {
                    if (!x.Children.Any(child => child.Value.Visible))
                    {
                        item.Visible = false;
                    }
                }
            });
        }

        protected abstract string GetCacheKey();

        protected abstract Task<TreeNode<MenuItem>> BuildAsync();

        public virtual void ResolveElementCount(TreeNode<MenuItem> curNode, bool deep = false)
        {
        }

        public virtual TreeNode<MenuItem> ResolveCurrentNode(ControllerContext context)
        {
            if (!_currentNodeResolved)
            {
                _currentNode = Root.SelectNode(x => x.Value.IsCurrent(context), true);
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

        public void ClearCache()
        {
            Services.Cache.RemoveByPattern(MENU_PATTERN_KEY.FormatInvariant(Name));
        }

        #region Utilities

        protected virtual bool ContainsProvider(string provider)
        {
            Guard.NotEmpty(provider, nameof(provider));

            if (_providers == null)
            {
                _providers = Root.GetMetadata<List<string>>("Providers") ?? new List<string>();
            }

            return _providers.Contains(provider);
        }

        protected virtual T GetRequestValue<T>(ControllerContext context, string name)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotEmpty(name, nameof(name));

            var value = context.RouteData?.Values[name]?.ToString();
            if (value.IsEmpty())
            {
                StringValues stringValues = new();
                context.HttpContext?.Request?.Form?.TryGetValue(name, out stringValues);
                value = stringValues.FirstOrDefault();

                if (value.IsEmpty())
                {
                    context.HttpContext?.Request?.Query?.TryGetValue(name, out stringValues);
                    value = stringValues.FirstOrDefault();
                }
            }

            if (CommonHelper.TryConvert<T>(value, out var result))
            {
                return result;
            }

            return default(T);
        }

        private async Task<bool> MenuItemAccessPermittedAsync(MenuItem item)
        {
            var result = true;

            if (item.PermissionNames.HasValue())
            {
                var permitted = await item
                    .PermissionNames
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .AnyAsync(async x => await Services.Permissions.AuthorizeByAliasAsync(x.Trim()));

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
