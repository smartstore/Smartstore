using System;
using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.Utilities;

namespace Smartstore.Engine
{
    public class DefaultLifetimeScopeAccessor : ILifetimeScopeAccessor
    {
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
                var scope = _httpContextAccessor.HttpContext?.GetServiceScope();

                if (scope == null)
                {
                    _state.SetState((scope = BeginLifetimeScope()));
                }

                return scope;
            }
        }

        public IDisposable BeginContextAwareScope()
        {
            // Stack-like behaviour for Non-HttpContext threads:
            // Only the first call returns a disposer, all nested calls to this method are void.
            var httpContext = _httpContextAccessor.HttpContext;

            return httpContext != null
                ? (IDisposable)ActionDisposable.Empty
                : new ContextAwareScope(
                    _state.GetState() == null
                        ? this.EndLifetimeScope
                        : (Action)null);
        }

        public void EndLifetimeScope()
        {
            if (_httpContextAccessor.HttpContext?.RequestServices != null)
            {
                // Don't end scopes in HttpContext
                return;
            }

            var scope = _state.GetState();
            if (scope != null)
            {
                scope.Dispose();
                _state.RemoveState();
            }
        }

        public ILifetimeScope BeginLifetimeScope(Action<ContainerBuilder> configurationAction = null)
        {
            return (configurationAction == null)
                ? _rootContainer.BeginLifetimeScope()
                : _rootContainer.BeginLifetimeScope(configurationAction);
        }

        class ContextAwareScope : IDisposable
        {
            private readonly Action _disposer;

            public ContextAwareScope(Action disposer)
            {
                _disposer = disposer;
            }

            public void Dispose()
            {
                _disposer?.Invoke();
            }
        }
    }
}