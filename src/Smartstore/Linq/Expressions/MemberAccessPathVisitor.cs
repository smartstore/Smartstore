using System.Reflection;
using System.Text;

namespace Smartstore.Linq.Expressions
{
    /// <summary>
    /// Inherits from the <see cref="ExpressionVisitor"/> base class and implements a expression visitor
    /// that builds up a path string that represents meber access in a Expression.
    /// </summary>
    public class MemberAccessPathVisitor : ExpressionVisitor
    {
        // StringBuilder instance that will store the path.
        private readonly Stack<string> _path = new();

        /// <summary>
        /// Gets the path analyzed by the visitor.
        /// </summary>
        public string Path
        {
            get
            {
                var sb = new StringBuilder();

                foreach (string path in _path)
                {
                    if (sb.Length == 0)
                        sb.Append(path);
                    else
                        sb.AppendFormat(".{0}", path);
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Overriden. Overrides all MemberAccess to build a path string.
        /// </summary>
        /// <param name="methodExp"></param>
        /// <returns></returns>
        protected override Expression VisitMemberAccess(MemberExpression methodExp)
        {
            if (methodExp.Member.MemberType != MemberTypes.Field && methodExp.Member.MemberType != MemberTypes.Property)
                throw new NotSupportedException("MemberAccessPathVisitor does not support a member access of type " +
                                                methodExp.Member.MemberType.ToString());

            _path.Push(methodExp.Member.Name);

            return base.VisitMemberAccess(methodExp);
        }

        /// <summary>
        /// Overriden. Throws a <see cref="NotSupportedException"/> when a method call is encountered.
        /// </summary>
        /// <param name="methodCallExp"></param>
        /// <returns></returns>
        protected override Expression VisitMethodCall(MethodCallExpression methodCallExp)
        {
            throw new NotSupportedException(
                "MemberAccessPathVisitor does not support method calls. Only MemberAccess expressions are allowed.");
        }
    }
}
