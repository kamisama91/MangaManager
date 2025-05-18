using CommandLine;
using System;

namespace MangaManager
{
    public class Options
    {
        [Option("input", Required = true, HelpText = "input folder")]
        public string SourceFolder { get; set; }

        [Option("output", Required = false, HelpText = "output")]
        public string ArchiveFolder { get; set; }

        [Option("quarantine", Required = false, HelpText = "quarantine")]
        public string QuarantineFolder { get; set; }

        [Option("data", Required = false, HelpText = "data folder")]
        public string DataFolder { get; set; }

        [Option("convert", Required = false, HelpText = "convert")]
        public bool ConvertRegular { get; set; }

        [Option("convert-back", Required = false, HelpText = "reverse convert rename")]
        public bool ConvertBack { get; set; }

        [Option("rename", Required = false, HelpText = "rename")]
        public bool Rename { get; set; }

        [Option("move", Required = false, HelpText = "move")]
        public bool Move { get; set; }

        [Option("scrap", Required = false, HelpText = "scrap")]
        public bool ScrapRegular { get; set; }

        [Option("scrap-no-prompt", Required = false, HelpText = "scrap auto ignore")]
        public bool ScrapAutoIgnore { get; set; }

        [Option("tag", Required = false, HelpText = "tag")]
        public bool TagRegular { get; set; }

        [Option("tag-force", Required = false, HelpText = "force tag")]
        public bool TagForce { get; set; }

        [Option("online-update", Required = false, HelpText = "online update")]
        public bool OnlineUpdate { get; set; }

        [Option("archive", Required = false, HelpText = "archive")]
        public bool Archive { get; set; }

        [Option("online-check", Required = false, HelpText = "check online library consistency")]
        public bool OnlineCheck { get; set; }

        public void ThowWhenNotValid()
        {
            if (ConvertRegular && ConvertBack)
            {
                throw new ApplicationException($"\"convert\" and \"convert-back\" are mutually exclusive options");
            }

            if (ScrapRegular && ScrapAutoIgnore)
            {
                throw new ApplicationException($"\"scrap\" and \"scrap-no-prompt\" are mutually exclusive options");
            }

            if (TagRegular && TagForce)
            {
                throw new ApplicationException($"\"tag\" and \"tag-force\" are mutually exclusive options");
            }
        }
    }
}
