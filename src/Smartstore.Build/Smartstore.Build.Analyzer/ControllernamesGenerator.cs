using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Smartstore.Build.Analyzer
{
    [Generator]
    public class ControllernamesGenerator : ISourceGenerator
    {
        private const string Controller = nameof(Controller);
        private const string ControllerNames = nameof(ControllerNames);

        private string GeneratedClassName { get; set; } = "";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register our syntax receiver
            context.RegisterForSyntaxNotifications(() => new ControllernamesSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // No syntax receiver?
            if (context.SyntaxReceiver == null || !(context.SyntaxReceiver is ControllernamesSyntaxReceiver syntaxReceiver))
            {
                return;
            }

            // get project root namespace
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? rootNamespace);

            if (rootNamespace != null)
            {
                // get last part of root namespace
                string rootNamespaceLastPart = rootNamespace.Split('.').Last();

                // We don't need a last part if it is Smartstore.Web
                if (rootNamespaceLastPart == "Web")
                {
                    rootNamespaceLastPart = "";
                }

                GeneratedClassName = $"{ControllerNames}{rootNamespaceLastPart}";

                var source = new StringBuilder();

                source.AppendLine("using System;");
                source.AppendLine($"namespace {rootNamespace} {{");
                source.AppendLine($"public static class {GeneratedClassName} {{");

                // we don't want to add them more than once
                var distinct = new List<string>();

                foreach (AnalyticalEntry? e in syntaxReceiver.Entries)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    string cds = e.ClassDeclaration!.Identifier.ToString();

                    // TODO: Check if it is really a controller class
                    if (cds.EndsWith(Controller) && !distinct.Contains(cds))
                    {
                        distinct.Add(cds);
                        cds = cds.Substring(0, cds.LastIndexOf(Controller));

                        source.AppendLine($"public const string {cds} = nameof({cds});");
                    }
                }

                source.AppendLine("}");
                source.AppendLine("}");

                // Collection is done, add it to the source
                context.AddSource($"{ControllerNames}{rootNamespaceLastPart}.g.cs", source.ToString());

                // now for the warnings output
                Diagnostics(context, syntaxReceiver);
            }
        }

        private void Diagnostics(GeneratorExecutionContext context, ControllernamesSyntaxReceiver syntaxReceiver)
        {
            foreach (AnalyticalEntry? entry in syntaxReceiver.Entries)
            {
                ClassDeclarationSyntax? classDeclaration = entry.ClassDeclaration;
                string className = classDeclaration!.Identifier.ToString();

                foreach (InvocationExpressionSyntax? inv in entry.Invocations)
                {
                    // return early if build is canceled
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    // Flag to determine if it is a Url.Action or RedirectToAction invocation
                    bool isUrlAction = inv.ToString().StartsWith("Url.Action");

                    SeparatedSyntaxList<ArgumentSyntax> arguments = inv.ArgumentList.Arguments;
                    if (arguments.Count >= 1)
                    {
                        // Check if first argument is a string literal
                        LiteralExpressionSyntax? arg0StringExpression = AsStringLiteralExpression(arguments[0].Expression);
                        InvocationExpressionSyntax? arg0InvocationExpression = AsInvocationExpression(arguments[0].Expression);
                        if (arg0StringExpression != null)
                        {
                            // SAN0001 + SAN0002
                            // First argument is a string literal
                            string text = arg0StringExpression.Token.ValueText.Replace("\"", "");
                            context.ReportDiagnostic(Diagnostic.Create(
                                isUrlAction ? DiagnosticDescriptors.UseNameofUrlAction : DiagnosticDescriptors.UseNameofRedirectToAction,
                                arg0StringExpression.GetLocation(), text));
                        }

                        if (arguments.Count >= 2)
                        {
                            // Check if second argument is string literal
                            LiteralExpressionSyntax? arg1StringExpression = AsStringLiteralExpression(arguments[1].Expression);
                            MemberAccessExpressionSyntax? arg1MemberExpression = AsSimpleMemberExpression(arguments[1].Expression);

                            if (arg1StringExpression != null)
                            {
                                // SAN0003 + SAN0004
                                // Second argument is a string literal
                                string controllerName = arg1StringExpression.Token.ValueText.Replace("\"", "");
                                context.ReportDiagnostic(Diagnostic.Create(
                                    isUrlAction
                                        ? DiagnosticDescriptors.UseControllerNamesUrlAction
                                        : DiagnosticDescriptors.UseControllerNamesRedirectToAction,
                                    arg1StringExpression.GetLocation(), controllerName));

                                if (arg0InvocationExpression != null &&
                                    arg0InvocationExpression.ToString() == "nameof" &&
                                    controllerName + Controller != className)
                                {
                                    if (!arg0InvocationExpression.ArgumentList.Arguments[0].ToString().StartsWith(controllerName + $"{Controller}."))
                                    {
                                        // SAN0005 + SAN0006
                                        context.ReportDiagnostic(Diagnostic.Create(
                                            isUrlAction
                                                ? DiagnosticDescriptors.UseSpecifiedClassUrlAction
                                                : DiagnosticDescriptors.UseSpecifiedClassRedirectToAction,
                                            arg0InvocationExpression.GetLocation()));
                                    }
                                }
                            }

                            // No string literal, let's check if it points to ControllerNames
                            if (arg1MemberExpression != null)
                            {
                                SimpleNameSyntax arg1MemberName = arg1MemberExpression.Name;

                                if (arg1MemberExpression.Expression.ToString() == GeneratedClassName)
                                {
                                    // is it pointing to another controller?
                                    if (arg0InvocationExpression != null &&
                                        arg0InvocationExpression.ToString().StartsWith("nameof"))
                                    {
                                        IdentifierNameSyntax? nameofIdentifier =
                                            AsIdentifierExpression(arg0InvocationExpression.ArgumentList.Arguments[0].Expression);

                                        if (arg1MemberName + Controller != className)
                                        {
                                            if (nameofIdentifier != null && arg1MemberName + Controller != className)
                                            {
                                                // SAN0005 + SAN0006
                                                // First argument (nameof) has no class specified
                                                context.ReportDiagnostic(Diagnostic.Create(
                                                    isUrlAction
                                                        ? DiagnosticDescriptors.UseSpecifiedClassUrlAction
                                                        : DiagnosticDescriptors.UseSpecifiedClassRedirectToAction,
                                                    arg0InvocationExpression.GetLocation()));
                                            }
                                        }

                                        MemberAccessExpressionSyntax? nameofMember =
                                            AsSimpleMemberExpression(arg0InvocationExpression.ArgumentList.Arguments[0].Expression);

                                        if (nameofMember != null)
                                        {
                                            string memberName = nameofMember.ToString();
                                            memberName = memberName.Substring(0, memberName.LastIndexOf(nameofMember.Name.ToString()) - 1);
                                            if (memberName != arg1MemberName + Controller)
                                            {
                                                // SAN0007 + SAN0008 (Error!)
                                                // First argument (nameof) and second argument (ControllerNames.xxx) point to different classes
                                                context.ReportDiagnostic(Diagnostic.Create(
                                                    isUrlAction
                                                        ? DiagnosticDescriptors.WrongClassUrlAction
                                                        : DiagnosticDescriptors.WrongClassRedirectToAction,
                                                    arg0InvocationExpression.GetLocation()));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private LiteralExpressionSyntax? AsStringLiteralExpression(ExpressionSyntax syntax)
        {
            if (syntax.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return (LiteralExpressionSyntax)syntax;
            }

            return null;
        }

        private InvocationExpressionSyntax? AsInvocationExpression(ExpressionSyntax syntax)
        {
            if (syntax.IsKind(SyntaxKind.InvocationExpression))
            {
                return (InvocationExpressionSyntax)syntax;
            }

            return null;
        }

        private MemberAccessExpressionSyntax? AsSimpleMemberExpression(ExpressionSyntax syntax)
        {
            if (syntax.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return (MemberAccessExpressionSyntax)syntax;
            }

            return null;
        }

        private IdentifierNameSyntax? AsIdentifierExpression(ExpressionSyntax syntax)
        {
            if (syntax.IsKind(SyntaxKind.IdentifierName))
            {
                return (IdentifierNameSyntax)syntax;
            }

            return null;
        }
    }
}