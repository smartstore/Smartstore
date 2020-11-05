using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data.Caching2
{
    /// <summary>
    /// A Table's EntityInfo and policy information.
    /// </summary>
    public class TableEntityInfo
    {
        /// <summary>
        /// Gets the CLR class that is used to represent instances of this type.
        /// Returns null if the type does not have a corresponding CLR class (known as a shadow type).
        /// </summary>
        public Type ClrType { set; get; }

        /// <summary>
        /// The Corresponding table's name.
        /// </summary>
        public string TableName { set; get; }

        /// <summary>
        /// Policy annotation.
        /// </summary>
        public CacheableEntityAttribute Policy { get; set; }

        /// <summary>
        /// Debug info.
        /// </summary>
        public override string ToString() => $"{ClrType}::{TableName}";
    }

    public sealed class CachingExpressionVisitor<TResult> : ExpressionVisitor
    {
        // Keys are both entity CLR type and table name (therefore object, not Type)
        private readonly ConcurrentDictionary<Type, Lazy<Dictionary<object, TableEntityInfo>>> _contextTableInfos =
            new ConcurrentDictionary<Type, Lazy<Dictionary<object, TableEntityInfo>>>();

        private readonly DbContext _context;

        private bool _isNoTracking;
        private bool _typesResolved;
        
        public CachingExpressionVisitor(DbContext context, bool async)
        {
            _context = context;
            IsAsyncQuery = async;
        }

        public bool IsAsyncQuery { get; }

        public Type SequenceType { get; private set; }

        public Type EntityType { get; private set; }

        public DbCachingPolicy CachingPolicy { get; private set; }

        private void EnsureTypesResolved()
        {
            if (_typesResolved)
            {
                return;
            }

            var resultType = typeof(TResult);

            if (IsAsyncQuery)
            {
                var typeDef = resultType.GetGenericTypeDefinition();
                if (typeDef == typeof(Task<>))
                {
                    // Single item query (First[OrDefault]Async, Single[OrDefault]Async)
                    EntityType = resultType.GetGenericArguments()[0];
                }
                else if (resultType.IsSubClass(typeof(IAsyncEnumerable<>), out var implType))
                {
                    // List query (ToListAsync, ToDictionaryAsync)
                    EntityType = resultType.GetGenericArguments()[0];
                    SequenceType = implType;
                }
            }
            else
            {
                if (resultType.IsSequenceType(out var entityType))
                {
                    // List query (ToList, ToDictionary)
                    EntityType = entityType;
                    SequenceType = typeof(IEnumerable<>).MakeGenericType(entityType);
                }
                else
                {
                    // Single item query (First[OrDefault], Single[OrDefault])
                    EntityType = resultType;
                }
            }

            _typesResolved = true;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsGenericMethod)
            {
                var methodDef = node.Method.GetGenericMethodDefinition();

                // Find cachable query extension calls
                if (methodDef == CachingQueryExtensions.AsCachingMethodInfo)
                {
                    // Get parameter with "last one wins"
                    CachingPolicy = node.Arguments
                        .OfType<ConstantExpression>()
                        .Where(a => a.Value is DbCachingPolicy)
                        .Select(a => (DbCachingPolicy)a.Value)
                        .Last();

                    if (CachingPolicy?.NoCaching == true)
                    {
                        CachingPolicy = null;
                    }

                    // Cut out extension expression
                    return Visit(node.Arguments[0]);
                }
                else if (!_isNoTracking && (methodDef == CachingQueryExtensions.AsNoTrackingMethodInfo || methodDef == CachingQueryExtensions.AsNoTrackingWithIdentityResolutionMethodInfo))
                {
                    // If _isNoTracking is true, we found the marker already. Useless to do it again.
                    if (node.Arguments.Count > 0)
                    {
                        var nodeType = node.Arguments[0]?.Type;
                        if (nodeType != null)
                        {
                            EnsureTypesResolved();

                            var nodeResultType = nodeType.GetGenericArguments()[0];
                            _isNoTracking = nodeResultType == EntityType;
                        }
                    }
                }
            }

            return base.VisitMethodCall(node);
        }

        public Expression ExtractPolicy(Expression expression)
        {
            _isNoTracking = false;
            _typesResolved = false;

            SequenceType = null;
            EntityType = null;
            CachingPolicy = null;

            var result = Visit(expression);

            if (!_isNoTracking)
            {
                CachingPolicy = null;
            }
            else
            {
                if (CachingPolicy?.NoCaching == true)
                {
                    CachingPolicy = null;
                }
                else if (EntityType != null)
                {
                    // TODO: (core) Handle toxic entities.
                    // TODO: (core) Handle option defaults.
                    var globalPolicyAttr = GetAllEntityInfos().Get(EntityType)?.Policy;
                    if (CachingPolicy == null)
                    {
                        CachingPolicy = new DbCachingPolicy(globalPolicyAttr);
                    }
                    //else
                    //{
                    //    CachingPolicy = new DbCachingPolicy 
                    //    {
                            
                    //    };
                    //}
                }
            }

            return result;
        }

        /// <summary>
        /// Returns all of the given context's entity infos.
        /// </summary>
        public Dictionary<object, TableEntityInfo> GetAllEntityInfos()
        {
            return _contextTableInfos.GetOrAdd(_context.GetType(),
                _ => new Lazy<Dictionary<object, TableEntityInfo>>(() =>
                {
                    var infos = new Dictionary<object, TableEntityInfo>();
                    foreach (var entityType in _context.Model.GetEntityTypes())
                    {
                        var clrType = entityType.ClrType;
                        var tableName = entityType.GetTableName();
                        var info = new TableEntityInfo
                        {
                            ClrType = clrType,
                            TableName = tableName,
                            Policy = clrType.GetAttribute<CacheableEntityAttribute>(false)
                        };

                        infos[clrType] = info;
                        infos[tableName] = info;
                    }
                    return infos;
                },
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }
    }
}
