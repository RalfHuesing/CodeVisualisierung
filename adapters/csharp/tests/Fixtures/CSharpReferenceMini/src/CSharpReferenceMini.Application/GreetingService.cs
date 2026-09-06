using CodeVisualisierung.CSharp.Fixtures.Contracts;

namespace CodeVisualisierung.CSharp.Fixtures.Application;

public sealed class Greeter : IGreeter
{
    public string Greet(string name) => $"Hello, {name}!";
}

public sealed class GreetingService(IGreeter greeter)
{
    public string CreateGreeting(string name) => greeter.Greet(name);
}
