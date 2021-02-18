using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Scheduling;
using Smartstore.Core.Security;
using Smartstore.Data;

namespace Smartstore.Core.Identity.Rules
{
    public partial class TargetGroupEvaluatorTask : ITask
    {
        protected readonly SmartDbContext _db;
        protected readonly IRuleService _ruleService;
        protected readonly ITargetGroupService _targetGroupService;
        protected readonly ICacheManager _cache;

        public TargetGroupEvaluatorTask(
            SmartDbContext db, 
            IRuleService ruleService,
            ITargetGroupService targetGroupService,
            ICacheManager cache)
        {
            _db = db;
            _ruleService = ruleService;
            _targetGroupService = targetGroupService;
            _cache = cache;
        }

        public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            //var count = 0;
            var numDeleted = 0;
            var numAdded = 0;
            var roleQuery = _db.CustomerRoles
                .Include(x => x.RuleSets)
                .AsNoTracking();

            if (ctx.Parameters.ContainsKey("CustomerRoleIds"))
            {
                var roleIds = ctx.Parameters["CustomerRoleIds"].ToIntArray();
                roleQuery = roleQuery.Where(x => roleIds.Contains(x.Id));

                // TODO: (mg) (core) Why not BatchDelete()? 
                numDeleted = await _db.Database.ExecuteSqlRawAsync(
                    "Delete From [dbo].[CustomerRoleMapping] Where [CustomerRoleId] In ({0}) And [IsSystemMapping] = 1",
                    new[] { string.Join(",", roleIds) },
                    cancellationToken: cancelToken);
            }
            else
            {
                numDeleted = await _db.Database.ExecuteSqlRawAsync("Delete From [dbo].[CustomerRoleMapping] Where [IsSystemMapping] = 1", cancellationToken: cancelToken);
            }

            var roles = await roleQuery
                .Where(x => x.Active && x.RuleSets.Any(y => y.IsActive))
                .ToListAsync(cancelToken);

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, hooksEnabled: false, deferCommit: true))
            {
                foreach (var role in roles)
                {
                    var ruleSetCustomerIds = new HashSet<int>();

                    // TODO: (mg) (core) Complete TargetGroupEvaluatorTask (TaskExecutionContext required).
                    //ctx.SetProgress(++count, roles.Count, $"Add customer assignments for role \"{role.SystemName.NaIfEmpty()}\".");

                    // Execute active rule sets and collect customer ids.
                    foreach (var ruleSet in role.RuleSets.Where(x => x.IsActive))
                    {
                        if (cancelToken.IsCancellationRequested)
                            return;

                        var expressionGroup = await _ruleService.CreateExpressionGroupAsync(ruleSet, _targetGroupService);
                        if (expressionGroup is FilterExpression expression)
                        {
                            var filterResult = _targetGroupService.ProcessFilter(expression, 0, 500);
                            var resultPager = new FastPager<Customer>(filterResult.SourceQuery, 500);

                            while ((await resultPager.ReadNextPageAsync(x => x.Id, x => x)).Out(out var customerIds))
                            {
                                ruleSetCustomerIds.AddRange(customerIds);
                            }
                        }
                    }

                    // Add mappings.
                    if (ruleSetCustomerIds.Any())
                    {
                        foreach (var chunk in ruleSetCustomerIds.Slice(500))
                        {
                            if (cancelToken.IsCancellationRequested)
                                return;

                            foreach (var customerId in chunk)
                            {
                                _db.CustomerRoleMappings.Add(new CustomerRoleMapping
                                {
                                    CustomerId = customerId,
                                    CustomerRoleId = role.Id,
                                    IsSystemMapping = true
                                });

                                ++numAdded;
                            }

                            await scope.CommitAsync(cancelToken);
                        }

                        try
                        {
                            scope.DbContext.DetachEntities<CustomerRoleMapping>();
                        }
                        catch { }
                    }
                }
            }

            if (numAdded > 0 || numDeleted > 0)
            {
                await _cache.RemoveByPatternAsync(AclService.ACL_SEGMENT_PATTERN);
            }

            Debug.WriteLineIf(numDeleted > 0 || numAdded > 0, $"Deleted {numDeleted} and added {numAdded} customer assignments for {roles.Count} roles.");
        }
    }
}
