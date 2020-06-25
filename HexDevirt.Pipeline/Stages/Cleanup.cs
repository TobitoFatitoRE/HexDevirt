using System.Linq;
using HexDevirt.Core;

namespace HexDevirt.Pipeline.Stages
{
    public class Cleanup : iStage
    {
        public string Name => "Cleanup Stage";
        public string Description => "Cleans up useless VM runtime data.";

        public void Execute(DevirtualizationCtx ctx)
        {
            foreach (var virtualizedMethod in ctx.VirtualizedMethods)
            {
                if (virtualizedMethod.Instructions != null && virtualizedMethod.Instructions.Count == 0)
                    continue;
                var vmAttribute = virtualizedMethod.Parent.CustomAttributes.First(q =>
                    q.Signature.FixedArguments.Count == 2 &&
                    q.Signature.FixedArguments[0].ArgumentType == ctx.Module.CorLibTypeFactory.String &&
                    q.Signature.FixedArguments[1].ArgumentType == ctx.Module.CorLibTypeFactory.Int32);
                virtualizedMethod.Parent.CustomAttributes.Remove(vmAttribute);
                var stream = ctx.Module.Resources.First(q => q.Name == virtualizedMethod.Id);
                ctx.Module.Resources.Remove(stream);
                if (ctx.Options.Verbose)
                    ctx.Logger.Success(
                        $"Removed vmAttribute And Resource Stream On Method [{virtualizedMethod.Parent.Name}]");
                virtualizedMethod.Parent.CilMethodBody.Instructions.OptimizeMacros();
            }
        }
    }
}