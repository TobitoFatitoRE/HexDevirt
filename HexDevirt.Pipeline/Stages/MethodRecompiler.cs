using HexDevirt.Core;

namespace HexDevirt.Pipeline.Stages
{
    public class MethodRecompiler : iStage
    {
        public string Name => nameof(MethodRecompiler);
        public string Description => "Recompile VMIL to CIL";

        public void Execute(DevirtualizationCtx ctx)
        {
            foreach (var virtualizedMethod in ctx.VirtualizedMethods)
            {
                if (virtualizedMethod.Instructions == null || virtualizedMethod.Instructions.Count == 0)
                {
                    ctx.Logger.Error($"Couldn't find any instructions for method [{virtualizedMethod.Parent.Name}]");
                    continue;
                }

                var recompiledBody = virtualizedMethod.CreateBody();
                virtualizedMethod.Parent.CilMethodBody = recompiledBody;
                if (ctx.Options.Verbose)
                    ctx.Logger.Success($"Recompiled CilMethodBody for method [{virtualizedMethod.Parent.Name}]");
            }
        }
    }
}