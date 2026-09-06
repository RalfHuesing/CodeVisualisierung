namespace CodeVisualisierung.CSharp.Tests.Infrastructure;

internal static class RoslynReferenceMiniSolutionSpec
{
    public static IReadOnlyList<RoslynProjectSpec> Create() =>
    [
        new RoslynProjectSpec(
            "CSharpReferenceMini.Contracts",
            [
                ("IGreeter.cs", """
                    namespace CodeVisualisierung.CSharp.Fixtures.Contracts;

                    public interface IGreeter
                    {
                        string Greet(string name);
                    }
                    """)
            ],
            VirtualProjectDirectory: "src/CSharpReferenceMini.Contracts"),
        new RoslynProjectSpec(
            "CSharpReferenceMini.Application",
            [
                ("Greeter.cs", """
                    using CodeVisualisierung.CSharp.Fixtures.Contracts;

                    namespace CodeVisualisierung.CSharp.Fixtures.Application;

                    public sealed class Greeter : IGreeter
                    {
                        public string Greet(string name) => $"Hello, {name}!";
                    }
                    """),
                ("GreetingService.cs", """
                    using CodeVisualisierung.CSharp.Fixtures.Contracts;

                    namespace CodeVisualisierung.CSharp.Fixtures.Application;

                    public sealed class GreetingService(IGreeter greeter)
                    {
                        public string CreateGreeting(string name) => greeter.Greet(name);
                    }
                    """)
            ],
            ProjectReferences: ["CSharpReferenceMini.Contracts"],
            VirtualProjectDirectory: "src/CSharpReferenceMini.Application")
    ];
}
