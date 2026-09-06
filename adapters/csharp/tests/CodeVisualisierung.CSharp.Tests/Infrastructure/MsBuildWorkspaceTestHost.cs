#nullable enable

using System.Collections.Concurrent;
using System.Threading;
using CodeVisualisierung.CSharp.Fixtures;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Xunit;

namespace CodeVisualisierung.CSharp.Tests.Infrastructure;

/// <summary>
/// Lädt die physische <c>CSharpReferenceMini</c>-Solution genau einmal über einen
/// echten <see cref="MSBuildWorkspace"/> und stellt sie für xUnit-v3-Assembly-Tests bereit.
/// </summary>
public sealed class MsBuildWorkspaceTestHost : IAsyncLifetime, IDisposable
{
    private readonly MSBuildWorkspace workspace;
    private readonly ConcurrentQueue<WorkspaceFailure> workspaceFailures = new();
    private Solution? solution;
    private int disposed;

    public MsBuildWorkspaceTestHost()
    {
        MsBuildLocatorRegistration.EnsureRegistered();
        workspace = MSBuildWorkspace.Create(CreateWorkspaceProperties());
        workspace.RegisterWorkspaceFailedHandler(OnWorkspaceFailed);
    }

    /// <summary>
    /// Die geladene physische Fixture-Solution.
    /// </summary>
    public Solution Solution => solution ?? throw new InvalidOperationException(
        $"{nameof(MsBuildWorkspaceTestHost)} wurde noch nicht initialisiert.");

    /// <summary>
    /// Absoluter Pfad zur geladenen Fixture-Solution.
    /// </summary>
    public string SolutionPath => CSharpReferenceMiniFixture.SolutionPath;

    /// <summary>
    /// Eine thread-sichere Momentaufnahme der während des Loads gemeldeten Workspace-Diagnosen.
    /// </summary>
    public IReadOnlyList<WorkspaceFailure> WorkspaceFailures => workspaceFailures.ToArray();

    public async ValueTask InitializeAsync()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) == 1, this);
        solution = await workspace.OpenSolutionAsync(SolutionPath);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }

        workspace.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void OnWorkspaceFailed(WorkspaceDiagnosticEventArgs eventArgs)
    {
        workspaceFailures.Enqueue(new WorkspaceFailure(
            eventArgs.Diagnostic.Kind.ToString(),
            eventArgs.Diagnostic.Message));
    }

    private static Dictionary<string, string> CreateWorkspaceProperties() => new()
    {
        ["DesignTimeBuild"] = "true",
        ["SkipCompilerExecution"] = "true",
        ["ProvideCommandLineArgs"] = "true",
        ["RunAnalyzers"] = "false",
        ["RunCodeAnalysis"] = "false"
    };
}

/// <summary>
/// Testlesbare Projektion einer Roslyn-Workspace-Diagnose.
/// </summary>
public sealed record WorkspaceFailure(string Kind, string Message);

internal static class MsBuildLocatorRegistration
{
    private static readonly Lazy<bool> Registration = new(
        Register,
        LazyThreadSafetyMode.ExecutionAndPublication);

    internal static void EnsureRegistered() => _ = Registration.Value;

    private static bool Register()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        return true;
    }
}
