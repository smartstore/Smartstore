using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Smartstore.Build.Analyzer
{
    public class AnalyticalEntry
    {
        public ClassDeclarationSyntax? ClassDeclaration { get; set; }

        public List<InvocationExpressionSyntax> Invocations { get; set; } = new List<InvocationExpressionSyntax>();

        public List<InvocationExpressionSyntax> RouteInvocations { get; set; } = new List<InvocationExpressionSyntax>();
    }
}