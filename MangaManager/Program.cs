using CommandLine;
using MangaManager.View;
using System;

namespace MangaManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default
                .ParseArguments<ProgramOptions>(args)
                .MapResult(
                    opts => Environment.ExitCode = Run(opts),
                    errs => Environment.ExitCode = -2);
        }
        
        public static ProgramOptions Options { get; private set; }
        public static ProgramView View { get; private set; }

        private static int Run(ProgramOptions options)
        {
            Options = options;
            View = new ProgramView();

            try
            {
            }
            catch (Exception)
            {
                return -1;
            }
            return 0;
        }
    }
}
