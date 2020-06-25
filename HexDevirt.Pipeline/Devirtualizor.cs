using System.Collections.Generic;
using HexDevirt.Core;
using HexDevirt.Pipeline.Stages;

namespace HexDevirt.Pipeline
{
    public class Devirtualizor
    {
        public Devirtualizor(DevirtualizationCtx ctx)
        {
            Ctx = ctx;
        }

        public List<iStage> Stages => new List<iStage>
        {
            new MethodDiscovery(),
            new MethodDissasembler(),
            new MethodRecompiler(),
            new VariableRecovery(),
            new Cleanup()
        };

        public DevirtualizationCtx Ctx { get; set; }

        public void Devirtualize()
        {
            foreach (var stage in Stages)
            {
                Ctx.Logger.Info($"Executing {stage.Name} stage...");
                stage.Execute(Ctx);
                Ctx.Logger.Success($"Executed {stage.Name} stage!");
            }
        }

        public void Write()
        {
            Ctx.Logger.Success("Wrote File!");
            Ctx.Module.Write(Ctx.OutPath);
        }
    }
}