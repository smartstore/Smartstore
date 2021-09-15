using System;
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
    }
}
