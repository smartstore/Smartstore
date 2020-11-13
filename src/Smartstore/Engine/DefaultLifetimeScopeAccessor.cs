using System;
using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.Utilities;

namespace Smartstore.Engine
{
    public class DefaultLifetimeScopeAccessor : ILifetimeScopeAccessor
    {
        internal static readonly object ScopeTag = "CustomScope";

        private readonly ContextState<ILifetimeScope> _state;
        private readonly ILifetimeScope _rootContainer;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultLifetimeScopeAccessor(IServiceProvider applicationServices, IHttpContextAccessor httpContextAccessor)
        {
            Guard.NotNull(applicationServices, nameof(applicationServices));
            Guard.NotNull(httpContextAccessor, nameof(httpContextAccessor));

            _rootContainer = applicationServices.AsLifetimeScope();
            _httpContextAccessor = httpContextAccessor;
            _state = new ContextState<ILifetimeScope>("CustomLifetimeScopeProvider.WorkScope");
        }

        public ILifetimeScope LifetimeScope
        { 
            get
            {
                var scope = _state.GetState();
                if (scope == null)
                {
                    scope = _state.SetState(_httpContextAccessor.HttpContext?.GetServiceScope() ?? CreateLifetimeScope());
                }

                return scope;
            }
            set
            {
                _state.SetState(value);
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
                scope = _state.GetState();
                if (scope == null)
                {
                    scope = _state.SetState(CreateLifetimeScope());
                    return new ActionDisposable(() => EndCurrentLifetimeScope());
                }

                return ActionDisposable.Empty;
            }
        }

        public void EndCurrentLifetimeScope()
        {
            var scope = _state.GetState();
            if (scope != null && scope.Tag == ScopeTag)
            {
                // Never dispose scopes which we didn't create here.
                scope.Dispose();
                _state.RemoveState();
            }
        }

        public ILifetimeScope CreateLifetimeScope(Action<ContainerBuilder> configurationAction = null)
        {   
            return (configurationAction == null)
                ? _rootContainer.BeginLifetimeScope(ScopeTag)
                : _rootContainer.BeginLifetimeScope(ScopeTag, configurationAction);
        }

        //class ContextAwareScope : IDisposable
        //{
        //    private readonly Action _disposer;

        //    public ContextAwareScope(Action disposer)
        //    {
        //        _disposer = disposer;
        //    }

        //    public void Dispose()
        //    {
        //        _disposer?.Invoke();
        //    }
        //}
    }
}