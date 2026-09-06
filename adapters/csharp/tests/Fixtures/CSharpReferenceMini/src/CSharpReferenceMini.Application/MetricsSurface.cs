namespace CodeVisualisierung.CSharp.Fixtures.Application;

public static class MetricsSurface
{
  public static int Classify(int value)
    {
        if (value > 0 && value < 10)
            return value;
      return value == 0 ? 1 : -1;
  }

  public static int ClassifyWithLocal(int value)
  {
    static bool IsPositive(int candidate)
    {
      if (candidate > 0)
        return true;
      return false;
    }

    return IsPositive(value) ? 1 : -1;
  }
}
