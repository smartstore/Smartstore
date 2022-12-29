using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Smartstore.Build.Analyzer
{
    public class ControllernamesSyntaxReceiver : ISyntaxReceiver
    {
        public List<AnalyticalEntry> Entries { get; private set; } = new List<AnalyticalEntry>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // We are only interested in class declarations
            // Hacky check for 'Controller', not type safe. Someone could create his class ending with Controller and not being a controller.

            if (syntaxNode is ClassDeclarationSyntax cds && cds.Identifier.ToString().EndsWith("Controller"))
            {
                AnalyticalEntry entry = new AnalyticalEntry();

                entry.ClassDeclaration = cds;

                // Search for any Url.Action and RedirectToAction invocations
                foreach (var invocationExpression in cds.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (invocationExpression.ToString().StartsWith("Url.Action") || invocationExpression.ToString().StartsWith("RedirectToAction"))
                    {
                        entry.Invocations.Add(invocationExpression);
                    }
                }

                Entries.Add(entry);
            }
        }
    }
}