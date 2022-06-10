using System.Reflection;
using Smartstore.Utilities;

namespace Smartstore.Linq
{
    public class LambdaPathExpander : IPathExpander
    {
        private readonly IList<LambdaExpression> _expands;

        public LambdaPathExpander()
        {
            _expands = new List<LambdaExpression>();
        }

        public IList<LambdaExpression> Expands => _expands;

        public virtual void Expand<T>(Expression<Func<T, object>> path)
        {
            Expand<T, T>(path);
        }

        public virtual void Expand<T, TTarget>(Expression<Func<TTarget, object>> path)
        {
            Guard.NotNull(path, "path");
            _expands.Add(path);
        }

        public void Expand<T>(string path)
        {
            Expand(typeof(T), path);
        }

        public virtual void Expand(Type type, string path)
        {
            Guard.NotNull(type, "type");
            Guard.NotEmpty(path, "path");

            Type t = type;

            // Wenn der Path durch einen Listen-Typ "unterbrochen" wird,
            // muss erst der ItemType der Liste gezogen und ab dem nächsten
            // Pfad ein neues Expand durchgeführt werden.

            var members = path.Tokenize('.');
            foreach (string member in members)
            {
                // Property ermitteln
                //MemberInfo prop = t.GetFieldOrProperty(member, true);
                var prop = t.GetProperty(member, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);

                if (prop == null)
                    throw new ArgumentException("The property or member '{0}' does not exist in type '{1}'.".FormatInvariant(member, t.FullName));

                Type memberType = prop.PropertyType;

                DoExpand(t, member);

                t = memberType.IsSequenceType()
                    ? TypeHelper.GetElementType(memberType)
                    : memberType;
            }
        }

        private void DoExpand(Type type, string path)
        {
            var entityParam = Expression.Parameter(type, "x"); // {x}
            path = String.Concat("x.", path.Trim('.'));

            var expression = System.Linq.Dynamic.Core.DynamicExpressionParser.ParseLambda(
                new ParameterExpression[] { entityParam },
                typeof(object),
                path.Trim('.'));

            _expands.Add(expression);
        }
    }
}
