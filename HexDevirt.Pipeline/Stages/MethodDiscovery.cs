using System.Collections.Generic;
using System.Linq;
using HexDevirt.Core;

namespace HexDevirt.Pipeline.Stages
{
    public class MethodDiscovery : iStage
    {
        public string Name => nameof(MethodDiscovery);
        public string Description => "Discover Virtualized Methods";

        public void Execute(DevirtualizationCtx ctx)
        {
            ctx.VirtualizedMethods = new List<VirtualizedMethod>();
            foreach (var type in ctx.Module.TopLevelTypes)
            foreach (var method in type.Methods.Where(q =>
                !q.IsNative && q.CilMethodBody != null && q.CilMethodBody.Instructions.Count >= 6 &&
                q.CustomAttributes.Count >= 1))
            {
                var vmAttribute = method.CustomAttributes.First(q =>
                    q.Signature.FixedArguments.Count == 2 &&
                    q.Signature.FixedArguments[0].ArgumentType == ctx.Module.CorLibTypeFactory.String &&
                    q.Signature.FixedArguments[1].ArgumentType == ctx.Module.CorLibTypeFactory.Int32);
                if (vmAttribute == null)
                    continue;
                var virtualizedMethod = new VirtualizedMethod(method,
                    (string) vmAttribute.Signature.FixedArguments[0].Element.Value,
                    (int) vmAttribute.Signature.FixedArguments[1].Element.Value);
                ctx.VirtualizedMethods.Add(virtualizedMethod);
                if (ctx.Options.Verbose)
                    ctx.Logger.Success(
                        $"Found Virtualized Method [{method.Name}] with ID [{virtualizedMethod.Id}] and Key [{virtualizedMethod.Key}]");
            }
        }
    }
}