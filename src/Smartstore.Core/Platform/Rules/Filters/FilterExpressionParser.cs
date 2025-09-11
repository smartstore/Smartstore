using System.Buffers;
using Parlot;
using Parlot.Fluent;
using Smartstore.Core.Rules.Operators;
using static Parlot.Fluent.Parsers;

namespace Smartstore.Core.Rules.Filters
{
    public static class FilterExpressionParser
    {
        static readonly SearchValues<char> _wildcardChars = SearchValues.Create("*?");
        static readonly SearchValues<char> _parentheseSearchValues = SearchValues.Create("()");
        static readonly Parser<IReadOnlyList<FilterExpression>> Grammar;

        #region Tokens

        // Groups
        static readonly Parser<char> LParen = Terms.Char('(');
        static readonly Parser<char> RParen = Terms.Char(')');

        // Operators
        static readonly Parser<char> IsNotEqualShort = Terms.Char('!');
        static readonly Parser<TextSpan> Op = Terms.Pattern(x => x == '~' || x == '=' || x == '!' || x == '<' || x == '>', 1, 2);
        #region Old
        //static readonly Parser<string> IsNotEqual = Terms.Text("!=");
        //static readonly Parser<string> IsEqual = Terms.Text("==");
        //static readonly Parser<string> NotContains = Terms.Text("!~");
        //static readonly Parser<string> GreaterOr = Terms.Text(">=");
        //static readonly Parser<string> LowerOr = Terms.Text("<=");
        //static readonly Parser<string> Different = Terms.Text("<>");
        //static readonly Parser<string> IsEqualShort = Terms.Text("=");
        //static readonly Parser<string> Contains = Terms.Text("~");
        //static readonly Parser<string> Greater = Terms.Text(">");
        //static readonly Parser<string> Lower = Terms.Text("<");
        //static readonly Parser<string> Op = IsNotEqual
        //    .Or(IsEqual)
        //    .Or(NotContains)
        //    .Or(GreaterOr)
        //    .Or(LowerOr)
        //    .Or(Different)
        //    .Or(IsEqualShort)
        //    .Or(IsNotEqualShort)
        //    .Or(Contains)
        //    .Or(Greater)
        //    .Or(Lower);
        #endregion

        // Logical Operators
        static readonly Parser<string> LogicalOr = Terms.Text("or", true);
        static readonly Parser<string> LogicalAnd = Terms.Text("and", true);

        // Term
        static readonly Func<char, bool> IsTermChar = c => !char.IsWhiteSpace(c) && c is not ('(' or ')');
        static readonly Parser<TextSpan> Term = Terms.Identifier(IsTermChar, IsTermChar);
        static readonly Parser<TextSpan> QuotedTerm = Terms.String(StringLiteralQuotes.SingleOrDouble);

        // Expression
        static readonly Deferred<IReadOnlyList<FilterExpression>> FilterExpressions = Deferred<IReadOnlyList<FilterExpression>>();
        static readonly Deferred<FilterExpression> FilterExpression = Deferred<FilterExpression>();
        static readonly Deferred<FilterExpression> GroupExpression = Deferred<FilterExpression>();

        #endregion

        static FilterExpressionParser()
        {
            // Initialize grammar
            // ------------------

            // Main expression parser: [op]Term [and|or], e.g.: ~banana and, !"john d?e"
            FilterExpression.Parser =
                // Optional equality operator (~ | ! | !~ | = | != | == | > | >= | < | <= | <>)
                ZeroOrOne(Op)
                // Optional whitespace between equality operator and term
                .AndSkip(ZeroOrOne(Literals.WhiteSpace()))
                // Term, either quoted or non-quoted
                .And(QuotedTerm.Or(Term))
                // Optional logical operator (AND | OR) after term. Default is "OR".
                .And(ZeroOrOne(LogicalOr.Or(LogicalAnd)))
                // Create FilterExpression result
                .Then(x =>
                {
                    // Item1: Operator
                    // Item2: Term
                    // Item3: Combinator
                    var op = ConvertOperator(x.Item1.ToString(), x.Item2);
                    var combinator = ConvertLogicalOperator(x.Item3);

                    return new FilterExpression
                    {
                        Operator = op,
                        LogicalOperator = combinator,
                        RawValue = x.Item2.ToString()
                    };
                });

            // Group expression parser: ([expressions...])
            GroupExpression.Parser =
                ZeroOrOne(IsNotEqualShort)
                // Opening left parent "("
                .AndSkip(LParen)
                // Skip "(" and find all inner expressions (0..n)
                .And(ZeroOrMany(GroupExpression.Or(FilterExpression)))
                // Skip ")" when reached
                .AndSkip(RParen)
                // Optional logical operator (AND | OR) after group. Default is "OR".
                .And(ZeroOrOne(LogicalOr.Or(LogicalAnd)))
                // Create FilterExpressionGroup result
                .Then<FilterExpression>(x =>
                {
                    var group = new FilterExpressionGroup(x.Item2.ToArray())
                    {
                        LogicalOperator = ConvertLogicalOperator(x.Item3),
                        IsSubGroup = true,
                        // If first token ("!") is present, then negate group
                        Value = x.Item1 == default(char)
                    };
                    return group;
                });

            // Root parser: finds grouped or ungrouped expressions.
            FilterExpressions.Parser = ZeroOrMany(GroupExpression.Or(FilterExpression));

            // (perf) Compile grammar
            Grammar = FilterExpressions.Compile();
        }

        public static bool TryParse<T, TValue>(Expression<Func<T, TValue>> memberExpression, string filter, out FilterExpression result)
             where T : class
        {
            Guard.NotNull(memberExpression);
            Guard.NotEmpty(filter);

            result = null;

            filter = NormalizeParentheses(filter);

            if (!Grammar.TryParse(filter, out var expressions))
            {
                return false;
            }

            var descriptor = new FilterDescriptor<T, TValue>(memberExpression);
            result = PostProcessResult(expressions, descriptor, typeof(T));

            return result != null;
        }

        public static FilterExpression Parse<T, TValue>(Expression<Func<T, TValue>> memberExpression, string filter)
            where T : class
        {
            Guard.NotNull(memberExpression);
            Guard.NotEmpty(filter);

            filter = NormalizeParentheses(filter);

            var expressions = Grammar.Parse(filter);
            var descriptor = new FilterDescriptor<T, TValue>(memberExpression);
            var result = PostProcessResult(expressions, descriptor, typeof(T));

            return result;
        }

        private static string NormalizeParentheses(string filter)
        {
            // Pseudocode plan:
            // - Early-exit if no parentheses present using IndexOfAny.
            // - Pre-scan once to detect if normalization is needed (unmatched ')' or leftover '(').
            // - If not needed, return original string to avoid allocations.
            // - If needed, allocate pooled char buffer and int stack (ArrayPool) sized to input length.
            // - Single pass:
            //   - Copy all chars, push positions of '(' onto stack, skip unmatched ')'.
            // - If all opens matched (stack empty), return new string from buffer slice.
            // - If opens unmatched remain, compact buffer by skipping positions recorded in stack,
            //   preserving order, then return new string from compacted buffer.
            // - Always return rented arrays in finally. No stackalloc used.

            // Fast path: no parentheses at all
            if (filter.AsSpan().IndexOfAny(_parentheseSearchValues) < 0)
            {
                return filter;
            }

            // Pre-scan: check if normalization is required
            int depth = 0;
            bool modified = false;

            foreach (var ch in filter)
            {
                if (ch == '(')
                {
                    depth++;
                }
                else if (ch == ')')
                {
                    if (depth == 0)
                    {
                        modified = true; // unmatched closing
                        // continue scanning to know if leftover opens exist, but result is already "modified"
                    }
                    else
                    {
                        depth--;
                    }
                }
            }

            if (depth > 0)
            {
                modified = true; // leftover unmatched openings
            }

            if (!modified)
            {
                // Parentheses are balanced and no unmatched closers: nothing to normalize
                return filter;
            }

            var charPool = ArrayPool<char>.Shared;
            var intPool = ArrayPool<int>.Shared;

            char[] buffer = null;
            int[] stack = null;

            try
            {
                int n = filter.Length;
                buffer = charPool.Rent(n);
                stack = intPool.Rent(n);

                int write = 0;
                int top = 0;

                // Build output while tracking '(' positions and skipping unmatched ')'
                for (int i = 0; i < n; i++)
                {
                    char ch = filter[i];
                    if (ch == '(')
                    {
                        stack[top++] = write;
                        buffer[write++] = ch;
                    }
                    else if (ch == ')')
                    {
                        if (top > 0)
                        {
                            top--;
                            buffer[write++] = ch;
                        }
                        // else: skip unmatched ')'
                    }
                    else
                    {
                        buffer[write++] = ch;
                    }
                }

                if (top == 0)
                {
                    // No unmatched '(' to remove
                    return new string(buffer, 0, write);
                }

                // Remove unmatched '(' recorded in stack[0..top-1] (ascending positions)
                int removeIdx = 0;
                int ri = 0, wi = 0;

                while (ri < write)
                {
                    if (removeIdx < top && ri == stack[removeIdx])
                    {
                        ri++;
                        removeIdx++;
                    }
                    else
                    {
                        buffer[wi++] = buffer[ri++];
                    }
                }

                return new string(buffer, 0, wi);
            }
            finally
            {
                if (buffer is not null)
                {
                    charPool.Return(buffer);
                }
                if (stack is not null)
                {
                    intPool.Return(stack);
                }
            }
        }

        private static FilterExpression PostProcessResult<T, TValue>(IReadOnlyList<FilterExpression> expressions, FilterDescriptor<T, TValue> descriptor, Type entityType)
            where T : class
        {
            PostProcess(expressions);

            return new FilterExpressionGroup(expressions.ToArray())
            {
                EntityType = entityType
            };

            void PostProcess(IEnumerable<FilterExpression> expressions2)
            {
                foreach (var expression in expressions2)
                {
                    // PostProcess expression
                    if (expression is FilterExpressionGroup group)
                    {
                        group.EntityType = entityType;
                        PostProcess(group.Expressions.Cast<FilterExpression>());
                    }
                    else
                    {
                        expression.Descriptor = descriptor;
                        expression.Value = expression.RawValue.Convert<TValue>();

                        if (expression.Operator is ContainsOperator op && typeof(TValue).IsNumericType())
                        {
                            // Fix default operator
                            expression.Operator = op.Negate ? RuleOperator.IsNotEqualTo : RuleOperator.IsEqualTo;
                        }
                    }
                }
            }
        }

        private static RuleOperator ConvertOperator(string op, TextSpan termSpan)
        {
            // The unquoted term
            var term = termSpan.ToString();
            var hasAnyWildcard = term != null && term.AsSpan().IndexOfAny(_wildcardChars) > -1;

            if (hasAnyWildcard)
            {
                if (op is (null or "~" or "=" or "=="))
                {
                    return RuleOperator.Like;
                }
                else if (op is ("!" or "!~" or "!=" or "<>"))
                {
                    return RuleOperator.NotLike;
                }
            }

            if (op.IsEmpty())
            {
                return RuleOperator.Contains;
            }

            switch (op)
            {
                case "=":
                case "==":
                    return RuleOperator.IsEqualTo;
                case "!":
                case "!=":
                case "<>":
                    return RuleOperator.IsNotEqualTo;
                case "!~":
                    return RuleOperator.NotContains;
                case ">":
                    return RuleOperator.GreaterThan;
                case ">=":
                    return RuleOperator.GreaterThanOrEqualTo;
                case "<":
                    return RuleOperator.LessThan;
                case "<=":
                    return RuleOperator.LessThanOrEqualTo;
                default:
                    return RuleOperator.Contains;

            }
        }

        private static LogicalRuleOperator ConvertLogicalOperator(string op)
        {
            return op == "and" ? LogicalRuleOperator.And : LogicalRuleOperator.Or;
        }
    }
}
