namespace CodeVisualisierung.CSharp.Fixtures.Application;

public partial class PartialCoordinator
{
    public int Value { get; set; }

    public void Increment()
    {
        Value = Value + 1;
    }
}
