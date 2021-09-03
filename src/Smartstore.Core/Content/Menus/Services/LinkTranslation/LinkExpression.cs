using System;
using System.Runtime.CompilerServices;

namespace Smartstore.Core.Content.Menus
{
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
            get => Target + Query.LeftPad(pad: '?');
        }

        public static LinkExpression Parse(string expression)
        {
            var result = new LinkExpression { RawExpression = expression };

            if (!TokenizeExpression(result) || string.IsNullOrWhiteSpace(result.Schema))
            {
                result.Schema = "url";
                result.Target = expression.EmptyNull();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TokenizeExpression(LinkExpression expression)
        {
            if (string.IsNullOrWhiteSpace(expression.RawExpression))
            {
                return false;
            }

            var colonIndex = expression.RawExpression.IndexOf(':');
            if (colonIndex > -1)
            {
                expression.Schema = expression.RawExpression.Substring(0, colonIndex).ToLower();
                if (expression.Schema.StartsWith("http"))
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
                expression.Query = expression.Target[(qmIndex + 1)..];
                expression.Target = expression.Target.Substring(0, qmIndex);
            }

            return true;
        }
    }
}
