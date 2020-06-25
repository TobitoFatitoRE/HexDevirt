using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using HexDevirt.Core;

namespace HexDevirt.Pipeline.Stages
{
    //TODO: Optimize
    //This is Experimental & can be disabled from options.
    public class VariableRecovery : iStage
    {
        public string Name => nameof(VariableRecovery);
        public string Description => "Recovers Variable Types";

        public void Execute(DevirtualizationCtx ctx)
        {
            if (!ctx.Options.RecoverVariableTypes)
                return;
            foreach (var method in ctx.VirtualizedMethods)
            {
                var parentMethod = method.Parent;
                CleanupUnusedVariables(parentMethod);
                RecoverVariableTypes(parentMethod);
                if (ctx.Options.Verbose)
                    foreach (var variable in method.Parent.CilMethodBody.LocalVariables.Where(q =>
                        q.VariableType != ctx.Module.CorLibTypeFactory.Object))
                        ctx.Logger.Success(
                            $"Recovered Variable Type [{variable.VariableType.Name}] on Variable Index [{variable.Index}] On Method [{method.Parent.Name}]");
            }
        }

        public static void RecoverVariableTypes(MethodDefinition method)
        {
            var instructionReferences = CalculateStuffenz(method, method.CilMethodBody.Instructions);
            for (var i = 0; i < method.CilMethodBody.Instructions.Count; i++)
                if (method.CilMethodBody.Instructions[i].IsStloc() &&
                    ((CilLocalVariable) method.CilMethodBody.Instructions[i].Operand).VariableType ==
                    method.Module.CorLibTypeFactory.Object)
                {
                    var references = instructionReferences.Where(q => q.Item1 == method.CilMethodBody.Instructions[i])
                        .ToList();
                    TypeSignature GoodSig = method.Module.CorLibTypeFactory.Object;
                    foreach (var d in references)
                    {
                        var type = GenerateType(method.Module, d.Item2, instructionReferences);
                        if (type != method.Module.CorLibTypeFactory.Object)
                        {
                            GoodSig = type;
                            break;
                        }
                    }

                    ((CilLocalVariable) method.CilMethodBody.Instructions[i].Operand).VariableType = GoodSig;
                }
        }

        public static TypeSignature GenerateType(ModuleDefinition moduleDefinition, object operand,
            List<(CilInstruction, object)> InstructionReferences)
        {
            if (operand is CilInstruction cilInstruction)
            {
                if (cilInstruction.IsLdarg()) return ((Parameter) cilInstruction.Operand).ParameterType;
                if (cilInstruction.IsLdloc())
                    if (cilInstruction.Operand is CilLocalVariable variable)
                        return variable.VariableType;
                if (cilInstruction.IsLdcI4()) return moduleDefinition.CorLibTypeFactory.Int32;
                switch (cilInstruction.OpCode.Code)
                {
                    case CilCode.Ldfld:
                    case CilCode.Ldflda:
                        return (cilInstruction.Operand as FieldDefinition)?.Signature.FieldType;
                    case CilCode.Ldc_R8:
                        return moduleDefinition.CorLibTypeFactory.Double;
                    case CilCode.Ldc_R4:
                        return moduleDefinition.CorLibTypeFactory.Single;
                    case CilCode.Ldstr:
                        return moduleDefinition.CorLibTypeFactory.String;
                    case CilCode.Ldarg:
                        return (cilInstruction.Operand as Parameter)?.ParameterType;
                    case CilCode.Newarr:
                        return (cilInstruction.Operand as ITypeDefOrRef)?.ToTypeSignature();
                    case CilCode.Call:
                    case CilCode.Callvirt:
                        if (cilInstruction.Operand is MethodDefinition method)
                            return method.Signature.ReturnType;
                        if (cilInstruction.Operand is MemberReference member)
                            if (member.Signature is MethodSignature sig)
                                return sig.ReturnType;
                        break;
                    case CilCode.Newobj:
                        if (cilInstruction.Operand is MethodDefinition methodd)
                            return methodd.DeclaringType.ToTypeSignature();

                        if (cilInstruction.Operand is MemberReference memberr)
                            return memberr.DeclaringType.ToTypeSignature();
                        break;
                    case CilCode.Box:
                        return (cilInstruction.Operand as ITypeDefOrRef)?.ToTypeSignature();

                    case CilCode.Conv_R4:
                        return moduleDefinition.CorLibTypeFactory.Single;
                    case CilCode.Conv_R8:
                        return moduleDefinition.CorLibTypeFactory.Double;

                    case CilCode.Clt:
                    case CilCode.Not:
                    case CilCode.And:
                    case CilCode.Shr:
                    case CilCode.Shl:
                    case CilCode.Rem:
                    case CilCode.Ceq:
                    case CilCode.Mul:
                    case CilCode.Cgt:
                    case CilCode.Add:
                    case CilCode.Sub:
                    case CilCode.Xor:
                    case CilCode.Div:
                    case CilCode.Dup:
                        var instr = InstructionReferences.First(q => q.Item1.Equals(cilInstruction)).Item2;
                        if (instr != null)
                            return GenerateType(moduleDefinition, instr,
                                InstructionReferences);
                        break;
                }
            }

            return moduleDefinition.CorLibTypeFactory.Object;
        }


        //Trying to emulate stack behavior and keep track of stack values
        //(Super buggy, will probably cause stack overflow on big methods with more branching)
        public static List<(CilInstruction, object)> CalculateStuffenz(MethodDefinition method,
            CilInstructionCollection Instructions, int index = 0, Stack<object> Stacc = null)
        {
            var finish = 0;
            if (index > 2)
                finish = index - 2;
            Instructions.CalculateOffsets();
            var InstructionReferences = new List<(CilInstruction, object)>();
            var Operands = new Stack<object>();
            if (Stacc != null)
                Operands = new Stack<object>(Stacc);

            var firstTiment = false;
            for (var i = index; i < Instructions.Count; i++)
            {
                if (firstTiment)
                    //**try to** prevent stack overflow
                    if (finish == i)
                        break;
                firstTiment = true;


                if (Instructions[i].IsUnconditionalBranch())
                {
                    i = Instructions.IndexOf(((CilInstructionLabel) Instructions[i].Operand).Instruction) - 1;
                    continue;
                }

                if (Instructions[i].IsConditionalBranch())
                {
                    //TODO: Optimize
                    //var branch1 = CalculateStuffenz(method, Instructions, i + 1, Operands);
                    //Hello.AddRange(branch1);
                    var branch2 = CalculateStuffenz(method, Instructions,
                        Instructions.IndexOf(((CilInstructionLabel) Instructions[i].Operand).Instruction), Operands);
                    InstructionReferences.AddRange(branch2);
                    continue;
                }

                if (Instructions[i].OpCode == CilOpCodes.Ret) break;

                var pushes = Instructions[i].GetStackPushCount();
                var pops = Instructions[i].GetStackPopCount(method.CilMethodBody);

                for (var p = 0; p < pops; p++)
                    InstructionReferences.Add((Instructions[i], Operands.Pop()));
                for (var p = 0; p < pushes; p++)
                    Operands.Push(Instructions[i]);
            }

            return InstructionReferences;
        }

        public static void CleanupUnusedVariables(MethodDefinition method)
        {
            for (var i = 0; i < method.CilMethodBody.LocalVariables.Count; i++)
            {
                var pushpops = Usages(method.CilMethodBody.LocalVariables[i], method.CilMethodBody);
                if (pushpops.Item1 == 0 && pushpops.Item2 == 0)
                    method.CilMethodBody.LocalVariables.RemoveAt(i);
            }
        }

        public static (int, int) Usages(CilLocalVariable Variable, CilMethodBody Body)
        {
            var F = (0, 0);
            for (var i = 0; i < Body.Instructions.Count; i++)
            {
                if (Body.Instructions[i].IsLdloc() && Body.Instructions[i].Operand == Variable) F.Item1++;

                if (Body.Instructions[i].IsStloc() && Body.Instructions[i].Operand == Variable) F.Item2++;
            }

            return F;
        }
    }
}