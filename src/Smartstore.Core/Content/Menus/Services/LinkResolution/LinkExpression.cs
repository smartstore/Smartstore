using System.Diagnostics;

namespace Smartstore.Core.Content.Menus
{
    /// <summary>
    /// The parsed representation of a link expression.
    /// </summary>
    [DebuggerDisplay("LinkExpression: {RawExpression}")]
    public class LinkExpression
    {
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
                result.Schema = "url";
                result.Target = expression.EmptyNull();
                result.Query = string.Empty;
            }

            return result;
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
                if (expression.Schema.StartsWith("http") || expression.Schema.EqualsNoCase("mailto"))
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
    }
}
