namespace CodeVisualisierung.CSharp.Fixtures.Application;

public partial class PartialCoordinator
{
    public PartialCoordinator()
    {
    }

    public int Value { get; set; }

    public int Computed
    {
        get
        {
            static int Normalize(int value) => value + 1;
            return Normalize(Value);
        }
    }

    private int increments;

    public void Increment()
    {
        increments++;
        Value = Value + 1;
    }
}
