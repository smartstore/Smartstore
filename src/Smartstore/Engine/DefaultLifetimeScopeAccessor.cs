using Autofac;
using Autofac.Core.Lifetime;
using Microsoft.AspNetCore.Http;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Engine
{
    public class DefaultLifetimeScopeAccessor : ILifetimeScopeAccessor
    {
        internal static readonly object ScopeTag = "CustomScope";

        private readonly ContextState<ILifetimeScope> _contextState;
        private readonly ILifetimeScope _rootContainer;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultLifetimeScopeAccessor(IServiceProvider applicationServices, IHttpContextAccessor httpContextAccessor)
        {
            Guard.NotNull(applicationServices, nameof(applicationServices));
            Guard.NotNull(httpContextAccessor, nameof(httpContextAccessor));

            _rootContainer = applicationServices.AsLifetimeScope();
            _httpContextAccessor = httpContextAccessor;
            _contextState = new ContextState<ILifetimeScope>("CustomLifetimeScopeProvider.WorkScope");
        }

        public ILifetimeScope LifetimeScope
        {
            get
            {
                var scope = _contextState.Get();
                if (scope == null)
                {
                    scope = _httpContextAccessor.HttpContext?.GetServiceScope();
                    if (scope != null)
                    {
                        scope.CurrentScopeEnding += OnScopeEnding;
                    }
                    else
                    {
                        scope = CreateLifetimeScope();
                    }

                    _contextState.Push(scope);
                }

                return scope;
            }
            set
            {
                _contextState.Push(value);
            }
        }

        public IDisposable BeginContextAwareScope(out ILifetimeScope scope)
        {
            // Stack-like behaviour for Non-HttpContext threads:
            // Only the first call returns a disposer, all nested calls to this method are void.
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                scope = httpContext.GetServiceScope();
                return ActionDisposable.Empty;
            }
            else
            {
                scope = _contextState.Get();
                if (scope == null)
                {
                    scope = _contextState.Push(CreateLifetimeScope());
                    // out param not allowed in anon method.
                    var scope2 = scope;
                    return new ActionDisposable(() => scope2.Dispose());
                }

                return ActionDisposable.Empty;
            }
        }

        public void EndCurrentLifetimeScope()
        {
            var scope = _contextState.Get();
            if (scope != null && scope.Tag == ScopeTag)
            {
                // Never dispose scopes which we didn't create here.
                scope.Dispose();
                _contextState.Remove();
            }
        }

        public ILifetimeScope CreateLifetimeScope(Action<ContainerBuilder> configurationAction = null)
        {
            var scope = (configurationAction == null)
                ? _rootContainer.BeginLifetimeScope(ScopeTag)
                : _rootContainer.BeginLifetimeScope(ScopeTag, configurationAction);

            scope.CurrentScopeEnding += OnScopeEnding;

            return scope;
        }

        private void OnScopeEnding(object sender, LifetimeScopeEndingEventArgs args)
        {
            _contextState.Remove();
        }
    }
}