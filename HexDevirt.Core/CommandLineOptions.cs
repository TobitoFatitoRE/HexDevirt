using CommandLine;

namespace HexDevirt.Core
{
    public class CommandLineOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('r', "recover-variable-types", Required = false,
            HelpText = "Recover Variable Types (Can cause stack overflow,errors).", Default = true)]
        public bool RecoverVariableTypes { get; set; }
    }
}