using Microsoft.CodeAnalysis;

namespace CodeVisualisierung.CSharp.Analysis;

/// <summary>Maps Roslyn and workspace diagnostics to stable German public text.</summary>
public static class CSharpDiagnosticMessages
{
    private static readonly IReadOnlyDictionary<string, string> CompilerDetails =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CS0006"] = "Eine referenzierte Metadatendatei wurde nicht gefunden.",
            ["CS0012"] = "Ein referenzierter Typ benötigt eine zusätzliche Assemblyreferenz.",
            ["CS0103"] = "Der angegebene Name ist im aktuellen Kontext nicht vorhanden.",
            ["CS0117"] = "Der angegebene Typ enthält kein passendes Mitglied.",
            ["CS0120"] = "Für das nichtstatische Mitglied ist eine Objektinstanz erforderlich.",
            ["CS0161"] = "Nicht alle Codepfade geben einen Wert zurück.",
            ["CS0168"] = "Eine deklarierte Variable wird nicht verwendet.",
            ["CS0219"] = "Eine zugewiesene Variable wird nicht verwendet.",
            ["CS0246"] = "Der angegebene Typ- oder Namespacename wurde nicht gefunden.",
            ["CS0266"] = "Für die Konvertierung ist eine explizite Umwandlung erforderlich.",
            ["CS1001"] = "Ein Bezeichner wurde erwartet.",
            ["CS1002"] = "Ein Semikolon wurde erwartet.",
            ["CS1003"] = "Ein Syntaxelement wurde erwartet.",
            ["CS1009"] = "Eine Escape-Sequenz ist ungültig.",
            ["CS1022"] = "Eine Typ- oder Namespacedefinition wurde erwartet.",
            ["CS1026"] = "Eine schließende Klammer wurde erwartet.",
            ["CS1031"] = "Ein Typ wurde erwartet.",
            ["CS1061"] = "Der angegebene Typ enthält kein passendes Mitglied.",
            ["CS1513"] = "Eine schließende geschweifte Klammer wurde erwartet.",
            ["CS1514"] = "Eine öffnende geschweifte Klammer wurde erwartet.",
            ["CS1519"] = "In der Deklaration wurde ein ungültiges Token gefunden.",
            ["CS1525"] = "Ein ungültiger Ausdruck wurde gefunden.",
            ["CS1998"] = "Die asynchrone Methode enthält keinen await-Ausdruck.",
            ["CS8600"] = "Eine mögliche Nullreferenz wird in einen nichtnullbaren Typ konvertiert.",
            ["CS8602"] = "Eine mögliche Nullreferenz wird dereferenziert.",
            ["CS8603"] = "Eine mögliche Nullreferenz wird zurückgegeben.",
            ["CS8604"] = "Ein mögliches Nullargument wird an einen nichtnullbaren Parameter übergeben.",
            ["CS8618"] = "Das nichtnullbare Mitglied muss beim Verlassen des Konstruktors einen Wert enthalten.",
            ["CS8625"] = "Der Nullwert kann nicht einem nichtnullbaren Typ zugewiesen werden."
        };

    /// <summary>Formats a workspace diagnostic without exposing localized Roslyn text.</summary>
    public static string Workspace(string kind, string detail) =>
        $"Arbeitsbereichsdiagnose ({WorkspaceKind(kind)}): Die Eingabe konnte im MSBuild-Arbeitsbereich nicht geladen werden.";

    /// <summary>Formats a compiler diagnostic with stable German classification and detail.</summary>
    public static string Compiler(DiagnosticSeverity severity, string id, string detail) =>
        $"{CompilerSeverity(severity)} {id}: {CompilerDetail(id)}";

    /// <summary>Formats a document failure without exposing localized exception text.</summary>
    public static string DocumentFailure(string documentPath) =>
        $"Dokument konnte nicht verarbeitet werden: Die Quelldatei konnte nicht analysiert werden ({documentPath}).";

    private static string WorkspaceKind(string kind) => kind switch
    {
        "Failure" => "Fehler",
        "Warning" => "Warnung",
        "Information" => "Hinweis",
        _ => "Meldung"
    };

    private static string CompilerSeverity(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "Compilerfehler",
        DiagnosticSeverity.Warning => "Compilerwarnung",
        DiagnosticSeverity.Info => "Compilerhinweis",
        _ => "Compilerdiagnose"
    };

    private static string CompilerDetail(string id) =>
        CompilerDetails.TryGetValue(id, out var detail)
            ? detail
            : "Der C#-Compiler hat eine Diagnose für den Quelltext gemeldet.";
}
