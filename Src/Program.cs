using System.Reflection;
using RT.CommandLine;
using RT.PostBuild;
using Zinga;

if (args.Length == 2 && args[0] == "--post-build-check")
    return PostBuildChecker.RunPostBuildChecks(args[1], Assembly.GetExecutingAssembly());

if (args.SequenceEqual([]))
    args = ["run"];

try
{
    return CommandLineParser.Parse<CommandLine>(args).Execute();
}
catch (CommandLineParseException ex)
{
    ex.WriteUsageInfoToConsole();
    return 1;
}
