using System;
using System.Linq;
using System.Reflection;
using RT.CommandLine;
using RT.PostBuild;
using RT.PropellerApi;
using RT.Util;

namespace Zinga
{
    [CommandLine]
    abstract class CommandLineBase
    {
        public abstract int Execute();

        public static void PostBuildCheck(IPostBuildReporter rep)
        {
            CommandLineParser.PostBuildStep<CommandLineBase>(rep, null);
        }
    }

    [CommandName("postbuild"), Undocumented]
    sealed class PostBuild : CommandLineBase
    {
        [IsPositional, IsMandatory, Undocumented]
        public string SourcePath = null;

        public override int Execute() => PostBuildChecker.RunPostBuildChecks(SourcePath, Assembly.GetExecutingAssembly());
    }

    [CommandName("run"), Documentation("Runs a standalone Zinga server.")]
    sealed class Run : CommandLineBase
    {
        [IsPositional]
        public string ConfigFile = null;

        public override int Execute()
        {
            PropellerUtil.RunStandalone(ConfigFile ?? @"D:\Daten\Config\Zinga.config.json", new ZingaPropellerModule(),
#if DEBUG
                true
#else
                false
#endif
            );
            return 0;
        }
    }
}
