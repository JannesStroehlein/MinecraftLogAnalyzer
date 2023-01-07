using CommandLine;
using CommandLine.Text;

namespace MinecraftLogAnalyzer
{
    internal class CommandLineOptions
    {
        [Value(0, MetaName = "logdir", HelpText = "The directory with the log files.", Required = true)]
        public string LogDirectory { get; set; }

        [Option('e', "exportDir", Required = false, HelpText = "Specifies the export directory where all data will be saved. If not specified, a export directory will be created in the log directory.")]
        public string ExportDir { get; set; }

        [Option('n', "noExport", Required = false, HelpText = "Disables export if this is specified")]
        public bool NoExport { get; set; }

        [Usage(ApplicationAlias = "MCLogAnalyzer")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                    new Example("Analyze logs and save the data", new CommandLineOptions { LogDirectory = @"C:\MClogs" }),
                    new Example("Analyze logs and save the data to a custom directory", new CommandLineOptions { LogDirectory = @"C:\MClogs", ExportDir = @"C:\exportedData" }),
                    new Example("Analyze logs without saving the data", new CommandLineOptions { LogDirectory = @"C:\MClogs", NoExport = true })
                };
            }
        }
    }
}
