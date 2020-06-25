using System.Collections.Generic;
using System.IO;
using AsmResolver.DotNet;

namespace HexDevirt.Core
{
    public class DevirtualizationCtx
    {
        public DevirtualizationCtx(string path, CommandLineOptions options, iLogger logger)
        {
            InPath = path;
            OutPath = Path.Combine(Path.GetDirectoryName(path),
                Path.GetFileNameWithoutExtension(path) + "-Devirtualized" + Path.GetExtension(path));
            Module = ModuleDefinition.FromFile(path);
            Options = options;
            Logger = logger;
        }

        public ModuleDefinition Module { get; set; }
        public CommandLineOptions Options { get; set; }
        public iLogger Logger { get; set; }
        public string InPath { get; set; }
        public string OutPath { get; set; }
        public List<VirtualizedMethod> VirtualizedMethods { get; set; }
    }
}