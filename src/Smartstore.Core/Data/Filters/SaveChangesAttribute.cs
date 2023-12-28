using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Data;

namespace Smartstore.Core.Data
{
    /// <summary>
    /// Saves all pending changes in a <see cref="DbContext"/> instance to the database
    /// after action method has been executed.
    /// </summary>
    /// <typeparam name="TContext">The type of context to save changes for.</typeparam>
    /// <remarks>
    /// Creates an instance of <see cref="SaveChangesAttribute{TContext}"/>.
    /// </remarks>
    /// <param name="saveChanges">Set to <c>false</c> to override any controller-level or global <see cref="SaveChangesAttribute{TContext}"/>.</param>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class SaveChangesAttribute<TContext>(bool saveChanges = true) : ActionFilterAttribute where TContext : DbContext
    {
        public Type DbContextType => typeof(TContext);

        public bool SaveChanges { get; } = saveChanges;

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var actionExecuted = await next();

            if (actionExecuted.Exception == null)
            {
                var overrideFilter = context.ActionDescriptor.FilterDescriptors
                    .Where(x => x.Scope == FilterScope.Action)
                    .Select(x => x.Filter)
                    .OfType<SaveChangesAttribute<TContext>>()
                    .FirstOrDefault();

                if (overrideFilter?.SaveChanges == false)
                {
                    return;
                }

                if (context.HttpContext.RequestServices.GetRequiredService(DbContextType) is HookingDbContext db)
                {
                    using (new DbContextScope(db, autoDetectChanges: false))
                    {
                        db.ChangeTracker.DetectChanges();
                        if (db.ChangeTracker.HasChanges())
                        {
                            await db.SaveChangesAsync();
                        }
                    }
                }
            }
        }
    }
}
