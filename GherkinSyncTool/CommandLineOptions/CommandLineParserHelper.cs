using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommandLine;
using CommandLine.Text;
using NLog;

namespace GherkinSyncTool.CommandLineOptions
{
    public static class CommandLineParserHelper
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);

        private static readonly string Heading = @$"{Program.Version}Quantori GherkinSyncTool is an open-source console 
                                application that synchronizes tests scenarios in Gherkin syntax (also known as feature files) 
                                with a test management system or any other destination. https://github.com/quantori/GherkinSyncTool";

        public static Type[] LoadCliVerbsTypes()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();
        }

        public static void DisplayHelp(ParserResult<object> parserResult, IEnumerable<Error> errs)
        {
            if (errs.IsVersion())
            {
                Log.Info(Program.Version);
                Environment.Exit(0);
            }

            Log.Info(HelpText.AutoBuild(parserResult, h =>
            {
                h.AutoVersion = false;
                h.AdditionalNewLineAfterOption = false;
                h.Heading = Heading;
                h.AddEnumValuesToHelpText = true;
                return h;
            }));
            Environment.Exit(0);
        }
    }
}
