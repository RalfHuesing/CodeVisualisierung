using CodeVisualisierung.CSharp.Fixtures.Application;
using Xunit;

namespace CodeVisualisierung.CSharp.Fixtures.Tests;

public sealed class GreetingServiceTests
{
    [Fact]
    public void Creates_greeting_through_project_reference()
    {
        var service = new GreetingService(new Greeter());

        Assert.Equal("Hello, Roslyn!", service.CreateGreeting("Roslyn"));
    }
}
