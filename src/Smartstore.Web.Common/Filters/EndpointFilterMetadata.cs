#nullable enable

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Web.Filters
{
    public class EndpointFilterMetadata<TFilter, TController> : EndpointFilterMetadata
        where TFilter : IFilterMetadata
        where TController : Controller
    {
        public EndpointFilterMetadata()
            : base(typeof(TFilter), typeof(TController))
        {
        }

        public EndpointFilterMetadata<TFilter, TController> ForAction(Expression<Func<TController, Task<IActionResult>>> actionSelector)
        {
            ForAction(Guard.NotNull(actionSelector).ExtractMethodInfo());
            return this;
        }

        public EndpointFilterMetadata<TFilter, TController> ForAction(Expression<Func<TController, IActionResult>> actionSelector)
        {
            ForAction(Guard.NotNull(actionSelector).ExtractMethodInfo());
            return this;
        }

        public override IFilterMetadata GetFilter()
        {
            if (_condition != null)
            {
                return new DefaultConditionalFilter<TFilter>(_condition) { Order = Order, IsReusable = _isReusable };
            }

            return new TypeFilterAttribute(FilterType) { Order = Order, IsReusable = _isReusable };
        }
    }


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

        public EndpointFilterMetadata IsReusable()
        {
            _isReusable = true;
            return this;
        }

        /// <summary>
        /// Type of filter implementation.
        /// </summary>
        public Type FilterType { get; internal set; }

        /// <summary>
        /// Type of controller that the filter is applied to.
        /// </summary>
        public Type ControllerType { get; }

        public EndpointFilterMetadata ForController(string controllerName)
        {
            Guard.NotEmpty(controllerName);

            _controllers ??= [];
            _controllers.Add(controllerName);

            return this;
        }

        public EndpointFilterMetadata ForController(Func<ControllerModel, bool> selector)
        {
            Guard.NotNull(selector);

            _controllers ??= [];
            _controllers.Add(selector);

            return this;
        }

        public bool MatchController(ControllerModel controllerModel)
        {
            Guard.NotNull(controllerModel);

            var isAssignable = ControllerType.IsAssignableFrom(controllerModel.ControllerType);

            if (_controllers == null || _controllers.Count == 0)
            {
                return isAssignable;
            }

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

            return false;
        }

        public EndpointFilterMetadata ForAction(string actionName)
        {
            Guard.NotEmpty(actionName);

            _actions ??= [];
            _actions.Add(actionName);

            return this;
        }

        public EndpointFilterMetadata ForAction(MethodInfo method)
        {
            Guard.NotNull(method);

            _actions ??= [];
            _actions.Add(method);

            return this;
        }

        public EndpointFilterMetadata ForAction(Func<ActionModel, bool> selector)
        {
            Guard.NotNull(selector);

            _actions ??= [];
            _actions.Add(selector);

            return this;
        }

        public bool IsControllerFilter()
            => _actions.IsNullOrEmpty();

        public bool MatchAction(ActionModel actionModel)
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
        public EndpointFilterMetadata When(Func<ActionContext, bool> condition)
        {
            _condition = Guard.NotNull(condition);
            return this;
        }

        /// <summary>
        /// Executes filter only when the request is a non-ajax request.
        /// </summary>
        public EndpointFilterMetadata WhenNonAjax()
        {
            return When(context => !context.HttpContext.Request.IsAjax());
        }

        /// <summary>
        /// Executes filter only when the request is a non-ajax GET request.
        /// </summary>
        public EndpointFilterMetadata WhenNonAjaxGet()
        {
            return When(context => context.HttpContext.Request.IsNonAjaxGet());
        }

        /// <summary>
        /// Gets the actual filter.
        /// </summary>
        public abstract IFilterMetadata GetFilter();
    }
}
