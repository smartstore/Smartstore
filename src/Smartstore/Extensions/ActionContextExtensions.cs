using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;

namespace Smartstore
{
    public static class ActionContextExtensions
    {
        /// <summary>
        /// Checks whether current dispatched controller is of type <typeparamref name="TController"/>.
        /// </summary>
        public static bool ControllerIs<TController>(this ActionContext context)
            where TController : Controller
        {
            if (context is ControllerContext controllerContext)
            {
                return typeof(TController).IsAssignableFrom(controllerContext.ActionDescriptor.ControllerTypeInfo);
            }

            return false;
        }

        /// <summary>
        /// Checks whether current dispatched controller action is given <paramref name="actionSelector"/>.
        /// </summary>
        public static bool ControllerIs<TController>(this ActionContext context, Expression<Action<TController>> actionSelector)
            where TController : Controller
        {
            Guard.NotNull(actionSelector, nameof(actionSelector));
            
            if (context is ControllerContext controllerContext)
            {
                if (typeof(TController).IsAssignableFrom(controllerContext.ActionDescriptor.ControllerTypeInfo))
                {
                    return controllerContext.ActionDescriptor.MethodInfo == actionSelector.ExtractMethodInfo();
                }
            }

            return false;
        }
    }
}
