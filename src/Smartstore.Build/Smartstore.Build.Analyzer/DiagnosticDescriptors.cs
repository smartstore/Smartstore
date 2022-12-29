// ---------------------------------------------------------------------------------------
// Solution: Smartstore
// Project: Smartstore.Build.Analyzer
// Filename: DiagnosticDescriptors.cs
// 
// Last modified: 2022-12-29 14:16
// Created:       2022-12-29 13:34
// 
// Copyright: 2021 Walter Wissing & Co
// ---------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Smartstore.Build.Analyzer
{
    internal static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor UseNameofUrlAction = new DiagnosticDescriptor(
            "SAN0001",
            "Use nameof for action name",
            "Consider using nameof for Url.Action calls",
            "Usage",
            DiagnosticSeverity.Warning,
            true
        );

        public static readonly DiagnosticDescriptor UseNameofRedirectToAction = new DiagnosticDescriptor(
            "SAN0002",
            "Use nameof for action name",
            "Consider using nameof for RedirectToAction calls",
            "Usage",
            DiagnosticSeverity.Warning,
            true
        );

        public static readonly DiagnosticDescriptor UseControllerNamesUrlAction = new DiagnosticDescriptor(
            "SAN0003",
            "Use ControllerNames constants for controller names",
            "Consider using ControllerNames for Url.Action calls",
            "Usage",
            DiagnosticSeverity.Warning,
            true
        );

        public static readonly DiagnosticDescriptor UseControllerNamesRedirectToAction = new DiagnosticDescriptor(
            "SAN0004",
            "Use ControllerNames constants for controller names",
            "Consider using ControllerNames for RedirectToAction calls",
            "Usage",
            DiagnosticSeverity.Warning,
            true
        );

        public static readonly DiagnosticDescriptor UseSpecifiedClassUrlAction = new DiagnosticDescriptor(
            "SAN0005",
            "Use specified class for action nameof",
            "Action parameter does not specify the correct class",
            "Usage",
            DiagnosticSeverity.Warning,
            true
        );

        public static readonly DiagnosticDescriptor UseSpecifiedClassRedirectToAction = new DiagnosticDescriptor(
            "SAN0006",
            "Use specified class for action nameof",
            "Action parameter does not specify the correct class",
            "Usage",
            DiagnosticSeverity.Warning,
            true
        );

        public static readonly DiagnosticDescriptor WrongClassUrlAction = new DiagnosticDescriptor(
            "SAN0007",
            "Use correct class for action nameof",
            "Mismatch of Action parameter and controller name",
            "Usage",
            DiagnosticSeverity.Error,
            true
        );

        public static readonly DiagnosticDescriptor WrongClassRedirectToAction = new DiagnosticDescriptor(
            "SAN0008",
            "Use correct class for action nameof",
            "Mismatch of Action parameter and controller name",
            "Usage",
            DiagnosticSeverity.Error,
            true
        );

    }
}