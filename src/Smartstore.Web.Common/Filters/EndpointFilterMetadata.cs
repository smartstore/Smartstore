#nullable enable

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Web.Filters
{
    /// <summary>
    /// Represents metadata for an endpoint filter that applies to a specific controller type and, optionally, actions.
    /// </summary>
    /// <typeparam name="TFilter">The type of the filter.</typeparam>
    /// <typeparam name="TController">The type of the controller. By default, any controller that inherits from TController is matched.</typeparam>
    public class EndpointFilterMetadata<TFilter, TController> : EndpointFilterMetadata
        where TFilter : IFilterMetadata
        where TController : Controller
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointFilterMetadata{TFilter, TController}"/> class.
        /// </summary>
        public EndpointFilterMetadata()
            : base(typeof(TFilter), typeof(TController))
        {
        }

        /// <summary>
        /// Specifies the action method to which the filter applies.
        /// Multiple calls to <c>ForAction</c> are additive. If no actions are specified, the filter applies to all actions (added at controller level).
        /// </summary>
        /// <param name="actionSelector">An expression that selects the action method.</param>
        /// <returns>The current <see cref="EndpointFilterMetadata{TFilter, TController}"/> instance.</returns>
        public EndpointFilterMetadata<TFilter, TController> ForAction(Expression<Func<TController, Task<IActionResult>>> actionSelector)
        {
            ForAction(Guard.NotNull(actionSelector).ExtractMethodInfo());
            return this;
        }

        /// <summary>
        /// Specifies the action method to which the filter applies.
        /// Multiple calls to <c>ForAction</c> are additive. If no actions are specified, the filter applies to all actions (added at controller level).
        /// </summary>
        /// <param name="actionSelector">An expression that selects the action method.</param>
        /// <returns>The current <see cref="EndpointFilterMetadata{TFilter, TController}"/> instance.</returns>
        public EndpointFilterMetadata<TFilter, TController> ForAction(Expression<Func<TController, IActionResult>> actionSelector)
        {
            ForAction(Guard.NotNull(actionSelector).ExtractMethodInfo());
            return this;
        }

        /// <summary>
        /// Gets the actual filter (either <see cref="ConditionalFilter" /> or <see cref="TypeFilterAttribute" />, depending on configuration).
        /// </summary>
        /// <returns>The filter metadata.</returns>
        internal override IFilterMetadata GetFilter()
        {
            if (_condition != null)
            {
                return new DefaultConditionalFilter<TFilter>(_condition) { Order = Order, IsReusable = _isReusable };
            }

            return new TypeFilterAttribute(FilterType) { Order = Order, IsReusable = _isReusable };
        }
    }


    /// <summary>
    /// Represents metadata for an endpoint filter.
    /// </summary>
    public abstract class EndpointFilterMetadata : IOrderedFilter
    {
        protected List<object>? _controllers;
        protected List<object>? _actions;
        protected Func<ActionContext, bool>? _condition;
        protected bool _isReusable;

        public EndpointFilterMetadata(Type filterType, Type controllerType)
        {
            Guard.NotNull(filterType);
            Guard.IsAssignableFrom<IFilterMetadata>(filterType);

            Guard.NotNull(controllerType);
            Guard.IsAssignableFrom<Controller>(controllerType);

            FilterType = filterType;
            ControllerType = controllerType;
        }

        /// <inheritdoc />
        public int Order { get; set; }

        /// <summary>
        /// Marks the filter as reusable.
        /// </summary>
        /// <returns>The current <see cref="EndpointFilterMetadata"/> instance.</returns>
        public EndpointFilterMetadata IsReusable()
        {
            _isReusable = true;
            return this;
        }

        /// <summary>
        /// Gets the type of the filter implementation.
        /// </summary>
        public Type FilterType { get; internal set; }

        /// <summary>
        /// Gets the type of the controller that the filter is applied to.
        /// </summary>
        public Type ControllerType { get; }

        /// <summary>
        /// Specifies the controller to which the filter applies.
        /// Multiple calls to <c>ForController</c> are additive. If no controllers are specified, 
        /// the filter applies to all controllers that are assignable from <see cref="ControllerType"/>.
        /// </summary>
        /// <param name="controllerName">The name of the controller.</param>
        /// <returns>The current <see cref="EndpointFilterMetadata"/> instance.</returns>
        public EndpointFilterMetadata ForController(string controllerName)
        {
            Guard.NotEmpty(controllerName);

            _controllers ??= [];
            _controllers.Add(controllerName);

            return this;
        }

        /// <summary>
        /// Specifies the controller to which the filter applies.
        /// Multiple calls to <c>ForController</c> are additive. If no controllers are specified, 
        /// the filter applies to all controllers that are assignable from <see cref="ControllerType"/>.
        /// </summary>
        /// <param name="selector">A function that selects applicable controllers.</param>
        /// <returns>The current <see cref="EndpointFilterMetadata"/> instance.</returns>
        public EndpointFilterMetadata ForController(Func<ControllerModel, bool> selector)
        {
            Guard.NotNull(selector);

            _controllers ??= [];
            _controllers.Add(selector);

            return this;
        }

        /// <summary>
        /// Determines whether the filter matches the specified controller model.
        /// </summary>
        /// <param name="controllerModel">The controller model.</param>
        /// <returns><c>true</c> if the filter matches the controller model; otherwise, <c>false</c>.</returns>
        internal bool MatchController(ControllerModel controllerModel)
        {
            Guard.NotNull(controllerModel);

            var isAssignable = ControllerType.IsAssignableFrom(controllerModel.ControllerType);

            if (_controllers == null || _controllers.Count == 0)
            {
                return isAssignable;
            }

            if (isAssignable)
            {
                foreach (var controller in _controllers)
                {
                    if (controller is string controllerName)
                    {
                        if (controllerName.EqualsNoCase(controllerModel.ControllerName))
                        {
                            return true;
                        }
                    }
                    else if (controller is Func<ControllerModel, bool> selector)
                    {
                        if (selector(controllerModel))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Specifies the action method to which the filter applies.
        /// Multiple calls to <c>ForAction</c> are additive. If no actions are specified, the filter applies to all actions (added at controller level).
        /// </summary>
        /// <param name="actionName">The name of the action method.</param>
        /// <returns>The current <see cref="EndpointFilterMetadata"/> instance.</returns>
        public EndpointFilterMetadata ForAction(string actionName)
        {
            Guard.NotEmpty(actionName);

            _actions ??= [];
            _actions.Add(actionName);

            return this;
        }

        /// <summary>
        /// Specifies the action method to which the filter applies.
        /// Multiple calls to <c>ForAction</c> are additive. If no actions are specified, the filter applies to all actions (added at controller level).
        /// </summary>
        /// <param name="method">The method information of the action method.</param>
        /// <returns>The current <see cref="EndpointFilterMetadata"/> instance.</returns>
        public EndpointFilterMetadata ForAction(MethodInfo method)
        {
            Guard.NotNull(method);

            _actions ??= [];
            _actions.Add(method);

            return this;
        }

        /// <summary>
        /// Specifies the action method to which the filter applies.
        /// Multiple calls to <c>ForAction</c> are additive. If no actions are specified, the filter applies to all actions (added at controller level).
        /// </summary>
        /// <param name="selector">A function that selects applicable action methods.</param>
        /// <returns>The current <see cref="EndpointFilterMetadata"/> instance.</returns>
        public EndpointFilterMetadata ForAction(Func<ActionModel, bool> selector)
        {
            Guard.NotNull(selector);

            _actions ??= [];
            _actions.Add(selector);

            return this;
        }

        /// <summary>
        /// Determines whether the filter is a controller filter.
        /// </summary>
        /// <returns><c>true</c> if the filter is a controller filter (has no actions); otherwise, <c>false</c>.</returns>
        internal bool IsControllerFilter()
            => _actions.IsNullOrEmpty();

        /// <summary>
        /// Determines whether the filter matches the specified action model.
        /// </summary>
        /// <param name="actionModel">The action model.</param>
        /// <returns><c>true</c> if the filter matches the action model; otherwise, <c>false</c>.</returns>
        internal bool MatchAction(ActionModel actionModel)
        {
            Guard.NotNull(actionModel);

            if (_actions == null || _actions.Count == 0)
            {
                return true;
            }

            foreach (var action in _actions)
            {
                if (action is MethodInfo method)
                {
                    if (method == actionModel.ActionMethod)
                    {
                        return true;
                    }
                }
                if (action is string actionName)
                {
                    if (actionName.EqualsNoCase(actionModel.ActionName))
                    {
                        return true;
                    }
                }
                else if (action is Func<ActionModel, bool> selector)
                {
                    if (selector(actionModel))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Sets a condition that must be met for the filter to be executed.
        /// </summary>
        /// <param name="condition">A function that defines the condition.</param>
        /// <returns>The current <see cref="EndpointFilterMetadata"/> instance.</returns>
        public EndpointFilterMetadata When(Func<ActionContext, bool> condition)
        {
            _condition = Guard.NotNull(condition);
            return this;
        }

        /// <summary>
        /// Executes the filter only when the request is a non-ajax request.
        /// </summary>
        /// <returns>The current <see cref="EndpointFilterMetadata"/> instance.</returns>
        public EndpointFilterMetadata WhenNonAjax()
        {
            return When(context => !context.HttpContext.Request.IsAjax());
        }

        /// <summary>
        /// Executes the filter only when the request is a non-ajax GET request.
        /// </summary>
        /// <returns>The current <see cref="EndpointFilterMetadata"/> instance.</returns>
        public EndpointFilterMetadata WhenNonAjaxGet()
        {
            return When(context => context.HttpContext.Request.IsNonAjaxGet());
        }

        /// <summary>
        /// Gets the actual filter.
        /// </summary>
        /// <returns>The filter metadata.</returns>
        internal abstract IFilterMetadata GetFilter();
    }
}
