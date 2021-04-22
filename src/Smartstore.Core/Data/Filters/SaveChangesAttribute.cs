using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Core.Data
{
    /// <summary>
    /// Saves all pending changes in a <see cref="DbContext"/> instance to the database
    /// after action method has been executed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class SaveChangesAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Creates an instance of <see cref="SaveChangesAttribute"/>.
        /// </summary>
        /// <param name="dbContextType">The type of context to save changes for.</param>
        public SaveChangesAttribute(Type dbContextType)
        {
            Guard.NotNull(dbContextType, nameof(dbContextType));
            Guard.IsAssignableFrom<DbContext>(dbContextType);

            DbContextType = dbContextType;
        }

        public Type DbContextType { get; }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // TODO: (core) Make option to suppress SaveChanges for a single action
            var actionExecuted = await next();

            if (actionExecuted.Exception == null)
            {
                var db = context.HttpContext.RequestServices.GetRequiredService(DbContextType) as DbContext;
                if (db != null)
                {
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
