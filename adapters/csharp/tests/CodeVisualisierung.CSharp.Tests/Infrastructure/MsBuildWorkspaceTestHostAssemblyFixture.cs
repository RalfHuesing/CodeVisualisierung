using Xunit;

// Der Host besitzt den Workspace und wird von xUnit-v3 als Assembly-Fixture
// genau einmal initialisiert und nach allen Tests wieder entsorgt.
[assembly: AssemblyFixture(typeof(CodeVisualisierung.CSharp.Tests.Infrastructure.MsBuildWorkspaceTestHost))]
