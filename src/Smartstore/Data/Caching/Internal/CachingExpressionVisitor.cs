using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Smartstore.Domain;

namespace Smartstore.Data.Caching.Internal
{
    /// <summary>
    /// A Table's EntityInfo and policy information.
    /// </summary>
    internal class TableEntityInfo
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

    internal sealed class CachingExpressionVisitor : ExpressionVisitor
    {
        // Key = DbContextType
        // Value = [Key: EntityClrType, ...]
        private readonly ConcurrentDictionary<Type, Lazy<Dictionary<Type, TableEntityInfo>>> _contextTableInfos =
            new ConcurrentDictionary<Type, Lazy<Dictionary<Type, TableEntityInfo>>>();

        private readonly DbContext _context;
        private readonly CachingOptionsExtension _extension;

        private bool _isNoTracking;

        public CachingExpressionVisitor(DbContext context, CachingOptionsExtension extension)
        {
            _context = context;
            _extension = extension;
        }

        public bool IsSequenceType { get; private set; }

        public Type ElementType { get; private set; }

        public DbCachingPolicy CachingPolicy { get; private set; }

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
                            var nodeResultType = nodeType.GetGenericArguments()[0];
                            _isNoTracking = nodeResultType == ElementType;
                        }
                    }
                }
            }

            return base.VisitMethodCall(node);
        }

        public Expression ExtractPolicy(Expression expression)
        {
            _isNoTracking = false;

            IsSequenceType = false;
            ElementType = expression.Type;
            CachingPolicy = null;

            if (expression.Type.IsEnumerableType(out var elementType))
            {
                IsSequenceType = true;
                ElementType = elementType;
            }

            expression = Visit(expression);

            if (!_isNoTracking && typeof(BaseEntity).IsAssignableFrom(ElementType))
            {
                // We never gonna cache trackable entities
                CachingPolicy = null;
            }
            else
            {
                CachingPolicy = SanitizePolicy(CachingPolicy);
            }

            return expression;
        }

        private DbCachingPolicy SanitizePolicy(DbCachingPolicy policy)
        {
            if (policy?.NoCaching == true)
            {
                // Caching disabled on query level
                return null;
            }

            // Try resolve global policy
            var policyAttribute = GetAllEntityInfos().Get(ElementType)?.Policy;

            if (policyAttribute != null)
            {
                if (policyAttribute.NeverCache)
                {
                    return null;
                }

                // Either create new policy from attribute or merge attribute with query policy.
                policy = (policy ?? new DbCachingPolicy()).Merge(policyAttribute);
            }

            if (policy != null)
            {
                // Global fallbacks from extension options
                if (policy.ExpirationTimeout == null)
                {
                    policy.ExpirationTimeout = _extension.DefaultExpirationTimeout;
                }

                if (policy.MaxRows == null)
                {
                    policy.MaxRows = _extension.DefaultMaxRows;
                }
            }

            return policy;
        }

        /// <summary>
        /// Returns all of the given context's entity infos.
        /// </summary>
        public Dictionary<Type, TableEntityInfo> GetAllEntityInfos()
        {
            return _contextTableInfos.GetOrAdd(_context.GetType(),
                _ => new Lazy<Dictionary<Type, TableEntityInfo>>(() =>
                {
                    var infos = new Dictionary<Type, TableEntityInfo>();
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
                    }
                    return infos;
                },
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }
    }
}
