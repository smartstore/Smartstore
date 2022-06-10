using Microsoft.AspNetCore.Mvc.Filters;

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
        /// <param name="saveChanges">Set to <c>false</c> to override any controller-level or global <see cref="SaveChangesAttribute"/>.</param>
        public SaveChangesAttribute(Type dbContextType, bool saveChanges = true)
        {
            Guard.NotNull(dbContextType, nameof(dbContextType));
            Guard.IsAssignableFrom<DbContext>(dbContextType);

            DbContextType = dbContextType;
            SaveChanges = saveChanges;
        }

        public Type DbContextType { get; }

        public bool SaveChanges { get; }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // TODO: (core) Make option to suppress SaveChanges for a single action
            var actionExecuted = await next();

            if (actionExecuted.Exception == null)
            {
                var overrideFilter = context.ActionDescriptor.FilterDescriptors
                    .Where(x => x.Scope == FilterScope.Action)
                    .Select(x => x.Filter)
                    .OfType<SaveChangesAttribute>()
                    .FirstOrDefault(x => x.DbContextType == DbContextType);

                if (overrideFilter?.SaveChanges == false)
                {
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService(DbContextType) as DbContext;
                if (db != null)
                {
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
