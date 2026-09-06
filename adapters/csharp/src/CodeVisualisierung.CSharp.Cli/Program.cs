using System.Text;

namespace CodeVisualisierung.CSharp.Cli;

internal static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        return CliApplication.Run(args, Console.Out, Console.Error);
    }
}
