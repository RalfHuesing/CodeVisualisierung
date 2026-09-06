namespace CodeVisualisierung.CSharp.Fixtures.Application;

public partial class PartialCoordinator
{
    public string Describe()
    {
        static string Format(int value) => value.ToString();
        return Format(Value);
    }
}
