using CommandLine;

namespace MangaManager
{
    public class ProgramOptions
    {
        [Option('i', "input", Required = true, HelpText = "input")]
        public string SourceFolder { get; set; }

        [Option('c', "convert", Required = false, HelpText = "convert")]
        public bool Convert { get; set; }

        [Option('r', "rename", Required = false, HelpText = "rename")]
        public bool Rename { get; set; }

        [Option('m', "move", Required = false, HelpText = "move")]
        public bool Move { get; set; }

        //[Option('s', "meta-source", Required = false, HelpText = "meta-source")]
        //public string MetadataSource { get; set; }

        //[Option('m', "meta", Required = false, HelpText = "meta")]
        //public bool Fetch { get; set; }

        [Option('t', "tag", Required = false, HelpText = "tag")]
        public bool Tag { get; set; }

        [Option('f', "force", Required = false, HelpText = "force tag")]
        public bool TagForce { get; set; }

        [Option('a', "archive", Required = false, HelpText = "archive")]
        public bool Archive { get; set; }

        [Option('o', "output", Required = false, HelpText = "output")]
        public string ArchiveFolder { get; set; }

        [Option('q', "quarantine", Required = false, HelpText = "quarantine")]
        public string QuarantineFolder { get; set; }

    }
}
