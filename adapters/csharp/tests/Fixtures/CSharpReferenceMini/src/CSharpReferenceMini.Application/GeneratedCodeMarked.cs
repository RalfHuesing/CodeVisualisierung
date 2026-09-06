using System.CodeDom.Compiler;

namespace CodeVisualisierung.CSharp.Fixtures.Generated;

[GeneratedCode("fixture", "1.0")]
public class GeneratedAttributeSurface
{
    public void GeneratedAttributeMethod()
    {
    }
}

public class MixedGeneratedSurface
{
    [GeneratedCode("fixture", "1.0")]
    public void GeneratedAttributeMember()
    {
    }

    public void OwnMember()
    {
    }
}
