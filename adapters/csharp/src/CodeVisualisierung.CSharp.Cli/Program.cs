using System.Text;

namespace CodeVisualisierung.CSharp.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        return await CliApplication.RunAsync(args, Console.Out, Console.Error);
    }
}
