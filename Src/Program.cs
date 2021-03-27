using RT.CommandLine;

namespace Zinga
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                return CommandLineParser.Parse<CommandLineBase>(args).Execute();
            }
            catch (CommandLineParseException ex)
            {
                ex.WriteUsageInfoToConsole();
                return 1;
            }
        }
    }
}
