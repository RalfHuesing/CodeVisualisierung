namespace CodeVisualisierung.CSharp.Fixtures.Application;

public interface IProcessor
{
    string Process(string input);
}

public class BaseProcessor
{
    public virtual string Process(string input) => input;
}

public sealed class DerivedProcessor : BaseProcessor, IProcessor
{
    public override string Process(string input) => input.Trim();
}

public static class OverloadAndGeneric
{
    public static int Convert(int value) => value;

    public static string Convert(string value) => value;

    public static T Echo<T>(T value) => value;
}

public readonly record struct ValueToken(int Value)
{
    public static ValueToken operator +(ValueToken left, ValueToken right) => new(left.Value + right.Value);
}

public struct MutableCounter
{
    public int Value;
}

public enum ProcessingState
{
    Pending,
    Done
}

public delegate string StringTransformer(string value);

public static class SemanticUseSite
{
    public static event Action? Executed;

    public static string Execute()
    {
        var processor = new DerivedProcessor();
        var token = new ValueToken(1);
        var converted = OverloadAndGeneric.Convert(token.Value);
        var coordinator = new PartialCoordinator();
        coordinator.Value = converted;
        StringTransformer transformer = value => processor.Process(value);
        Executed?.Invoke();

        static string Format(string value) => value.Trim();

        return transformer(Format(coordinator.Describe()));
    }
}
