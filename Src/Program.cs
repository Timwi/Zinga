using RT.CommandLine;
using Zinga;

try
{
    return CommandLineParser.Parse<CommandLineBase>(args).Execute();
}
catch (CommandLineParseException ex)
{
    ex.WriteUsageInfoToConsole();
    return 1;
}
