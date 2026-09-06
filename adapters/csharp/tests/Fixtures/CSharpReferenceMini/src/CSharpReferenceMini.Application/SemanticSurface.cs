namespace CodeVisualisierung.CSharp.Fixtures.Application;

public interface IProcessor
{
    string Process(string input);
}

public interface IParentProcessor
{
    string Process(string input);
}

public interface IChildProcessor : IParentProcessor
{
}

public sealed class InterfaceChildProcessor : IChildProcessor
{
    public string Process(string input) => input;
}

public class BaseProcessor
{
    public virtual string Process(string input) => input;
}

public sealed class DerivedProcessor : BaseProcessor, IProcessor
{
    public override string Process(string input) => input.Trim();
}

public class EventBase
{
    public virtual event Action? Changed;

    protected void RaiseChanged() => Changed?.Invoke();
}

public sealed class EventDerived : EventBase
{
    public override event Action? Changed;

    private void RaiseDerivedChanged() => Changed?.Invoke();
}

public sealed class Box<T>
{
}

public static class OverloadAndGeneric
{
    public static int Convert(int value) => value;

    public static string Convert(string value) => value;

    public static T Echo<T>(T value) => value;

    public static T First<T>(T value) => value;

    public static T Second<T>(T value) => value;
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

    private static void SetValue(out int value) => value = 1;

    private static void TouchValue(ref int value) => value++;

    private static void ObserveValue(in int value)
    {
        _ = value;
    }

    public static void UseOut()
    {
        var counter = new MutableCounter();
        SetValue(out counter.Value);
    }

    public static void UseRef()
    {
        var counter = new MutableCounter();
        TouchValue(ref counter.Value);
    }

    public static void UseIn()
    {
        var counter = new MutableCounter();
        ObserveValue(in counter.Value);
    }

    public static void UseNestedType()
    {
        _ = new Box<Box<int>>();
    }
}
