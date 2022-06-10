using System.Reflection;
using Smartstore.Domain;

namespace Smartstore.Data.Caching.Internal
{
    /// <summary>
    /// Extracts all involved entity sets from an axpression.
    /// </summary>
    internal class DependencyVisitor : ExpressionVisitor
    {
        public HashSet<Type> Types { get; set; } = new HashSet<Type>();

        public Expression ExtractDependencies(Expression expression)
        {
            Types.Clear();

            return Visit(expression);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            AddType(node.Type);
            return base.VisitBlock(node);
        }

        protected override Expression VisitDefault(DefaultExpression node)
        {
            AddType(node.Type);
            return base.VisitDefault(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            AddType(node.Type);
            return base.VisitLambda(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            AddType(node.Type);
            return base.VisitNew(node);
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            AddType(node.Type);
            return base.VisitNewArray(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.TypeAs:
                    AddType(node.Type);
                    break;
            }

            return base.VisitUnary(node);
        }

        protected override Expression VisitIndex(IndexExpression node)
        {
            if (node.Indexer != null)
            {
                HandleMember(node.Object);
            }

            return base.VisitIndex(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            HandleMember(node.Expression);
            return base.VisitMember(node);
        }

        private void HandleMember(Expression instance)
        {
            if (instance is ConstantExpression constExpression)
            {
                AddType(constExpression.Type);
            }
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            AddType(node.Type);
            return base.VisitConstant(node);
        }

        private void AddType(Type type)
        {
            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsGenericType)
            {
                foreach (var genericArg in type.GetGenericArguments())
                {
                    var argType = genericArg.GetTypeInfo();
                    if (argType.IsGenericType)
                    {
                        AddType(genericArg);
                    }
                    else
                    {
                        TryAdd(argType);
                    }
                }
            }
            else
            {
                TryAdd(typeInfo);
            }

            void TryAdd(Type type)
            {
                if (typeof(BaseEntity).IsAssignableFrom(type))
                {
                    Types.Add(type);
                }
            }
        }
    }
}