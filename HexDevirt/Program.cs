using System;
using CommandLine;
using HexDevirt.Core;
using HexDevirt.Pipeline;
using Console = Colorful.Console;

namespace HexDevirt
{
    internal class Program
    {
        public static Version CurrentVersion = new Version("1.0.0");

        private static void Main(string[] args)
        {
            var logger = new Logger();
            if (args.Length == 0)
            {
                logger.Error("This is a command line executable.");
                Console.ReadKey(true);
                Environment.Exit(0);
            }

            logger.ShowInfo(CurrentVersion);

            var options = (Parsed<CommandLineOptions>) Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(o =>
                {
                    if (o.Verbose) logger.Info("Verbose Output Enabled!");
                });

            var ctx = new DevirtualizationCtx(args[0], options.Value, logger);
            var devirt = new Devirtualizor(ctx);
            if (options.Value.Verbose)
                for (var i = 0; i < devirt.Stages.Count; i++)
                    logger.Info($"Stage[{i + 1}] ({devirt.Stages[i].Name}) - ({devirt.Stages[i].Description})");

            devirt.Devirtualize();
            devirt.Write();
            Console.ReadLine();
        }
    }
}