// ---------------------------------------------------------------------------------------
// Solution: Smartstore
// Project: Smartstore.Build.Analyzer
// Filename: AnalyticalEntry.cs
// 
// Last modified: 2022-12-29 12:01
// Created:       2022-12-29 12:01
// 
// Copyright: 2021 Walter Wissing & Co
// ---------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Smartstore.Build.Analyzer
{
    public class AnalyticalEntry
    {
        public ClassDeclarationSyntax? ClassDeclaration { get; set; }

        public List<InvocationExpressionSyntax> Invocations { get; set; } = new List<InvocationExpressionSyntax>();

    }
}