using CommandLine;

namespace MangaManager
{
    public class ProgramOptions
    {
        [Option('i', "input", Required = true, HelpText = "input")]
        public string SourceFolder { get; set; }
    }
}
