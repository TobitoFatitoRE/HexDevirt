using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace HexDevirt.Core
{
    public static class MethodRecompilerHelper
    {
        public static CilMethodBody CreateBody(this VirtualizedMethod virtualizedMethod)
        {
            var body = new CilMethodBody(virtualizedMethod.Parent);

            for (var i = 0; i < virtualizedMethod.Instructions.Count; i++)
                body.Instructions.Add(CreateInstruction(virtualizedMethod, virtualizedMethod.Instructions[i]));
            body.Instructions.CalculateOffsets();

            var locals = body.Instructions.Where(q => q.IsStloc() || q.IsLdloc()).ToList();
            var highest = 0;
            if (locals.Count > 0)
                highest = (int) locals.OrderByDescending(q => (int) q.Operand)
                    .First().Operand + 1;
            for (var i = 0; i < highest; i++)
                body.LocalVariables.Add(
                    new CilLocalVariable(virtualizedMethod.Parent.Module.CorLibTypeFactory.Object));
            for (var i = 0; i < body.Instructions.Count; i++)
                switch (body.Instructions[i].OpCode.Code)
                {
                    case CilCode.Br:
                    case CilCode.Brfalse:
                    case CilCode.Brtrue:
                        body.Instructions[i].Operand =
                            body.Instructions[(int) body.Instructions[i].Operand].CreateLabel();
                        break;
                    case CilCode.Ldloc:
                    case CilCode.Stloc:
                        body.Instructions[i].Operand = body.LocalVariables[(int) body.Instructions[i].Operand];
                        break;
                    case CilCode.Ldarg:
                    case CilCode.Starg:
                        body.Instructions[i].Operand =
                            virtualizedMethod.Parent.Parameters.GetBySignatureIndex((int) body.Instructions[i].Operand);
                        break;
                }

            body.ComputeMaxStack();
            return body;
        }

        public static CilInstruction ResolveByType(vmInstruction instr)
        {
            //If you have a better way lmk :D
            var type = instr.Operand;
            if (type is null) return new CilInstruction(CilOpCodes.Ldnull);
            if (type is int) return new CilInstruction(CilOpCodes.Ldc_I4, instr.Operand);
            if (type is float) return new CilInstruction(CilOpCodes.Ldc_R4, instr.Operand);
            if (type is double) return new CilInstruction(CilOpCodes.Ldc_R8, instr.Operand);
            if (type is string) return new CilInstruction(CilOpCodes.Ldstr, instr.Operand);
            if (type is long) return new CilInstruction(CilOpCodes.Ldc_I8, instr.Operand);
            return new CilInstruction(CilOpCodes.Nop);
        }

        public static CilInstruction CreateInstruction(VirtualizedMethod method, vmInstruction vmInstruction)
        {
            CilInstruction instruction;
            switch (vmInstruction.OpCode)
            {
                case vmOpCode.VmCall:
                    instruction = new CilInstruction(CilOpCodes.Call,
                        method.Parent.Module.LookupMember(
                            int.Parse(((string) vmInstruction.Operand).Substring(2))));
                    break;
                case vmOpCode.VmLdc:
                    instruction = ResolveByType(vmInstruction);
                    break;
                case vmOpCode.VmArray:
                    instruction = (int) vmInstruction.Operand switch
                    {
                        0 => new CilInstruction(CilOpCodes.Ldelem),
                        1 => new CilInstruction(CilOpCodes.Stelem),
                        _ => new CilInstruction(CilOpCodes.Nop)
                    };
                    break;
                case vmOpCode.VmLoc:
                    instruction = int.Parse(((string) vmInstruction.Operand)[0].ToString()) switch
                    {
                        0 => new CilInstruction(CilOpCodes.Ldloc),
                        _ => new CilInstruction(CilOpCodes.Stloc)
                    };
                    instruction.Operand = int.Parse(((string) vmInstruction.Operand).Substring(1));
                    break;
                case vmOpCode.VmArg:
                    instruction = int.Parse(((string) vmInstruction.Operand)[0].ToString()) switch
                    {
                        0 => new CilInstruction(CilOpCodes.Ldarg),
                        _ => new CilInstruction(CilOpCodes.Starg)
                    };
                    instruction.Operand = int.Parse(((string) vmInstruction.Operand).Substring(1));
                    break;
                case vmOpCode.VmFld:
                    instruction =
                        new CilInstruction(CilOpCodes.Ldfld,
                            method.Parent.Module.LookupMember(
                                int.Parse(((string) vmInstruction.Operand).Substring(1))));
                    break;
                case vmOpCode.VmConv:
                    instruction = (int) vmInstruction.Operand switch
                    {
                        0 => new CilInstruction(CilOpCodes.Conv_R4),
                        1 => new CilInstruction(CilOpCodes.Conv_R8),
                        _ => new CilInstruction(CilOpCodes.Nop)
                    };
                    break;
                case vmOpCode.Ldtoken:
                    instruction = new CilInstruction(CilOpCodes.Ldtoken,
                        method.Parent.Module.LookupMember(int.Parse(((string) vmInstruction.Operand).Substring(1))));
                    break;
                case vmOpCode.Brfalse:
                    instruction = new CilInstruction(CilOpCodes.Brfalse, vmInstruction.Operand);
                    break;
                case vmOpCode.Brtrue:
                    instruction = new CilInstruction(CilOpCodes.Brtrue, vmInstruction.Operand);
                    break;
                case vmOpCode.Br:
                    instruction = new CilInstruction(CilOpCodes.Br, vmInstruction.Operand);
                    break;
                case vmOpCode.Newobj:
                    instruction = new CilInstruction(CilOpCodes.Newobj,
                        method.Parent.Module.LookupMember((int) vmInstruction.Operand));
                    break;
                case vmOpCode.Box:
                    instruction = new CilInstruction(CilOpCodes.Box,
                        new ReferenceImporter(method.Parent.Module).ImportType(GetTypeDefOrRef(method.Parent.Module,
                            (string) vmInstruction.Operand)));
                    break;
                default:
                    instruction = new CilInstruction(Convert(vmInstruction.OpCode));
                    break;
            }

            return instruction;
        }

        //OOF
        public static ITypeDefOrRef GetTypeDefOrRef(ModuleDefinition def, string name)
        {
            foreach (var asmref in def.AssemblyReferences)
            {
                var xD = def.MetadataResolver.AssemblyResolver.Resolve(asmref);
                foreach (var type in xD.Modules.First().GetAllTypes())
                    if (type.FullName.Contains(name))
                        return type;
            }

            return null;
        }


        public static CilOpCode Convert(vmOpCode opCode)
        {
            return opCode switch
            {
                vmOpCode.AClt => CilOpCodes.Clt,
                vmOpCode.ANeg => CilOpCodes.Neg,
                vmOpCode.ANot => CilOpCodes.Not,
                vmOpCode.AAnd => CilOpCodes.And,
                vmOpCode.AShr => CilOpCodes.Shr,
                vmOpCode.AShl => CilOpCodes.Shl,
                vmOpCode.ARem => CilOpCodes.Rem,
                vmOpCode.ACeq => CilOpCodes.Ceq,
                vmOpCode.AMul => CilOpCodes.Mul,
                vmOpCode.ANop => CilOpCodes.Nop,
                vmOpCode.ACgt => CilOpCodes.Cgt,
                vmOpCode.AAdd => CilOpCodes.Add,
                vmOpCode.ASub => CilOpCodes.Sub,
                vmOpCode.ARet => CilOpCodes.Ret,
                vmOpCode.AXor => CilOpCodes.Xor,
                vmOpCode.APop => CilOpCodes.Pop,
                vmOpCode.ALdlen => CilOpCodes.Ldlen,
                vmOpCode.ADup => CilOpCodes.Dup,
                vmOpCode.ADiv => CilOpCodes.Div,
                _ => CilOpCodes.Nop
            };
        }
    }
}