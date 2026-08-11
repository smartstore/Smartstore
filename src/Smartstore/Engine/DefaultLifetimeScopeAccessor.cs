using System.Runtime.CompilerServices;
using Autofac;
using Autofac.Core.Lifetime;
using Microsoft.AspNetCore.Http;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Engine;

public class DefaultLifetimeScopeAccessor : ILifetimeScopeAccessor
{
    internal static readonly object ScopeTag = "CustomScope";

    private static readonly object EndingMarker = new();

    // Global registry of scopes whose CurrentScopeEnding event has fired.
    // Weak-keyed so GC'd scopes are reclaimed automatically without manual cleanup.
    // This catches independent AsyncLocal branches — tasks that called StartAsyncFlow()
    // before the scope was pushed and therefore hold their own ScopeHolder instance
    // that cannot be reached by nulling the holder on the disposing thread.
    private readonly ConditionalWeakTable<ILifetimeScope, object> _endingScopes = new();

    private readonly ContextState<ScopeHolder> _contextState;
    private readonly ILifetimeScope _rootContainer;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DefaultLifetimeScopeAccessor(IServiceProvider applicationServices, IHttpContextAccessor httpContextAccessor)
    {
        Guard.NotNull(applicationServices);
        Guard.NotNull(httpContextAccessor);

        _rootContainer = applicationServices.AsLifetimeScope();
        _httpContextAccessor = httpContextAccessor;
        _contextState = new ContextState<ScopeHolder>("CustomLifetimeScopeProvider.WorkScope");
    }

    public ILifetimeScope LifetimeScope
    {
        get
        {
            var holder = _contextState.Get();
            var scope = holder?.Scope;

            // Two-layer staleness check:
            //   scope == null    → ScopeHolder was already nulled by EndCurrentLifetimeScope
            //                      or OnScopeEnding on a branch that shares this holder
            //                      (fast path, no table lookup needed).
            //   IsScopeEnding   → scope is still set in an independent ScopeHolder, but
            //                      CurrentScopeEnding has already fired and the scope is
            //                      registered in _endingScopes (independent-branch fallback).
            if (scope == null || IsScopeEnding(scope))
            {
                if (scope != null)
                {
                    // Stale reference in an independent AsyncLocal branch — evict it so
                    // this branch does not keep handing out a disposing/disposed scope.
                    _contextState.Remove();
                }

                scope = _httpContextAccessor.HttpContext?.GetServiceScope();
                if (scope != null)
                {
                    scope.CurrentScopeEnding += OnScopeEnding;
                }
                else
                {
                    scope = CreateLifetimeScope();
                }

                _contextState.Push(new ScopeHolder(scope));
            }

            return scope;
        }
        set
        {
            _contextState.Push(value == null ? null : new ScopeHolder(value));
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

        var holder = _contextState.Get();
        scope = holder?.Scope;

        if (scope == null || IsScopeEnding(scope))
        {
            if (scope != null)
            {
                _contextState.Remove();
            }

            scope = CreateLifetimeScope();
            _contextState.Push(new ScopeHolder(scope));

            var scope2 = scope;
            return new ActionDisposable(() => scope2.Dispose());
        }

        return ActionDisposable.Empty;
    }

    public void EndCurrentLifetimeScope()
    {
        var holder = _contextState.Get();
        var scope = holder?.Scope;

        if (scope != null && scope.Tag == ScopeTag)
        {
            // Null the shared holder BEFORE calling Dispose() so that tasks sharing this
            // exact ScopeHolder instance see the invalidation immediately — even before
            // CurrentScopeEnding fires and populates _endingScopes via OnScopeEnding.
            holder.Scope = null;

            scope.Dispose();        // → triggers OnScopeEnding: populates _endingScopes, removes ContextState entry
            _contextState.Remove(); // belt-and-suspenders: OnScopeEnding runs on the disposing thread's
                                    // context and may not match the current ContextState branch
        }
    }

    public ILifetimeScope CreateLifetimeScope(Action<ContainerBuilder> configurationAction = null)
    {
        var scope = configurationAction == null
            ? _rootContainer.BeginLifetimeScope(ScopeTag)
            : _rootContainer.BeginLifetimeScope(ScopeTag, configurationAction);

        scope.CurrentScopeEnding += OnScopeEnding;

        return scope;
    }

    private bool IsScopeEnding(ILifetimeScope scope)
        => _endingScopes.TryGetValue(scope, out _);

    private void OnScopeEnding(object sender, LifetimeScopeEndingEventArgs args)
    {
        // Step 1 — global fallback: register before any teardown work so that independent
        // AsyncLocal branches pick up the signal via IsScopeEnding() on their next access.
        _endingScopes.TryAdd(args.LifetimeScope, EndingMarker);

        // Step 2 — shared-holder fast path: null the ScopeHolder if it still points to this
        // scope. Tasks sharing this specific ScopeHolder instance immediately see Scope = null
        // on their next read without requiring an _endingScopes lookup.
        var holder = _contextState.Get();
        if (holder != null && ReferenceEquals(holder.Scope, sender))
        {
            holder.Scope = null;
        }

        // Step 3 — remove the entry from the current AsyncLocal branch.
        _contextState.Remove();
    }

    /// <summary>
    /// Mutable wrapper around an <see cref="ILifetimeScope"/> stored in <see cref="ContextState{T}"/>.
    /// Because <see cref="ContextState{T}"/> keeps a reference to this object in a shared
    /// <see cref="AsyncLocal{T}"/> dictionary, all execution-context branches that inherited
    /// the same dictionary entry also observe mutations to <see cref="Scope"/> — most importantly
    /// the null-out performed before or during scope disposal.
    /// The backing field is <see langword="volatile"/> to guarantee cross-thread visibility of
    /// the null-write without requiring a full memory barrier or lock.
    /// </summary>
    private sealed class ScopeHolder(ILifetimeScope scope)
    {
        private volatile ILifetimeScope _scope = scope;

        public ILifetimeScope Scope
        {
            get => _scope;
            set => _scope = value;
        }
    }
}