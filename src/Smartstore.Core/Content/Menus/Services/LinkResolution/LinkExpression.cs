using System.Diagnostics;

namespace Smartstore.Core.Content.Menus
{
    /// <summary>
    /// The parsed representation of a link expression.
    /// </summary>
    [DebuggerDisplay("LinkExpression: {RawExpression}")]
    public class LinkExpression : ICloneable<LinkExpression>
    {
        private static readonly string[] _knownSchemas = new[] { "http", "https", "mailto", "javascript" };

        public string RawExpression { get; private set; }

        public string Schema { get; private set; }
        public string Target { get; private set; }
        public string Query { get; private set; }

        public string SchemaAndTarget
        {
            get => Schema + Target.LeftPad(pad: ':');
        }

        public string TargetAndQuery
        {
            get => Target + Query;
        }

        public LinkExpression ChangeTarget(string target)
            => Parse(Schema + target.EmptyNull().LeftPad(null, ':') + Query);

        public static LinkExpression Parse(string expression)
        {
            var result = new LinkExpression { RawExpression = expression.TrimSafe() };

            if (!TokenizeExpression(result) || string.IsNullOrWhiteSpace(result.Schema))
            {
                result.ConvertToUrl();
            }

            return result;
        }

        internal LinkExpression ConvertToUrl()
        {
            Schema = "url";
            Target = RawExpression.EmptyNull();
            Query = string.Empty;

            return this;
        }

        private static bool TokenizeExpression(LinkExpression expression)
        {
            if (string.IsNullOrWhiteSpace(expression.RawExpression))
            {
                return false;
            }

            var colonIndex = expression.RawExpression.IndexOf(':');
            if (colonIndex > -1)
            {
                expression.Schema = expression.RawExpression[..colonIndex].ToLower();
                if (_knownSchemas.Any(x => x.EqualsNoCase(expression.Schema)))
                {
                    expression.Schema = null;
                    colonIndex = -1;
                }
            }
            else
            {
                expression.Schema = string.Empty;
            }

            expression.Target = expression.RawExpression[(colonIndex + 1)..];

            var qmIndex = expression.Target.IndexOf('?');
            if (qmIndex > -1)
            {
                expression.Query = expression.Target[qmIndex..];
                expression.Target = expression.Target[..qmIndex];
            }
            else
            {
                expression.Query = string.Empty;
            }

            return true;
        }

        public override string ToString()
            => RawExpression;

        public LinkExpression Clone()
        {
            return new LinkExpression
            {
                RawExpression = RawExpression,
                Schema = Schema,
                Target = Target,
                Query = Query
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
