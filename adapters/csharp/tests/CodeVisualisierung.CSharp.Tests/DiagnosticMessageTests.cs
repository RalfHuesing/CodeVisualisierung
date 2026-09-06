using System.Globalization;
using CodeVisualisierung.CSharp.Analysis;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CodeVisualisierung.CSharp.Tests;

public sealed class DiagnosticMessageTests
{
    [Fact]
    public void EnglishWorkspaceCompilerAndDocumentDetailsKeepStableGermanDiagnostics()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            Assert.Equal(
                "Arbeitsbereichsdiagnose (Fehler): Die Eingabe konnte im MSBuild-Arbeitsbereich nicht geladen werden.",
                CSharpDiagnosticMessages.Workspace("Failure", "The project file could not be loaded."));
            Assert.Equal(
                "Compilerfehler CS1001: Ein Bezeichner wurde erwartet.",
                CSharpDiagnosticMessages.Compiler(DiagnosticSeverity.Error, "CS1001", "Identifier expected"));
            Assert.Equal(
                "Compilerwarnung CS8602: Eine mögliche Nullreferenz wird dereferenziert.",
                CSharpDiagnosticMessages.Compiler(DiagnosticSeverity.Warning, "CS8602", "Dereference of a possibly null reference."));
            Assert.Equal(
                "Dokument konnte nicht verarbeitet werden: Die Quelldatei konnte nicht analysiert werden (src\\Broken.cs).",
                CSharpDiagnosticMessages.DocumentFailure("src\\Broken.cs"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
