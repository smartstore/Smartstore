#nullable enable

using System.Reflection;
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
            Guard.NotNull(actionSelector);

            _actionMethods ??= [];
            _actionMethods.Add(actionSelector.ExtractMethodInfo());

            return this;
        }

        public EndpointFilterMetadata<TFilter, TController> ForAction(Expression<Func<TController, IActionResult>> actionSelector)
        {
            Guard.NotNull(actionSelector);

            _actionMethods ??= [];
            _actionMethods.Add(actionSelector.ExtractMethodInfo());

            return this;
        }

        public override IFilterMetadata GetFilter()
        {
            if (_condition != null)
            {
                return new DefaultConditionalFilter<TFilter>(_condition) { Order = Order };
            }

            return new TypeFilterAttribute(FilterType) { Order = Order };
        }
    }


    public abstract class EndpointFilterMetadata : IOrderedFilter
    {
        protected Func<ActionContext, bool>? _condition;
        protected List<MethodInfo>? _actionMethods;

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
        /// Type of filter implementation.
        /// </summary>
        public Type FilterType { get; internal set; }

        /// <summary>
        /// Type of controller that the filter is applied to.
        /// </summary>
        public Type ControllerType { get; }

        /// <summary>
        /// Methods of the controller that the filter is applied to.
        /// </summary>
        public IReadOnlyList<MethodInfo>? ActionMethods 
        {
            get => _actionMethods;
        }

        public EndpointFilterMetadata ForAction(MethodInfo method)
        {
            Guard.NotNull(method);

            _actionMethods ??= [];
            _actionMethods.Add(method);

            return this;
        }

        /// <summary>
        /// Sets a condition that must be met for the filter to be executed
        /// </summary>
        public EndpointFilterMetadata When(Func<ActionContext, bool> condition)
        {
            _condition = Guard.NotNull(condition);
            return this;
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
