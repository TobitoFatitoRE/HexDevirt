using System.Collections.Generic;
using System.IO;
using System.Linq;
using HexDevirt.Core;

namespace HexDevirt.Pipeline.Stages
{
    public class MethodDissasembler : iStage
    {
        public string Name => nameof(MethodDissasembler);
        public string Description => "Dissasembles the VMIL Instructions";

        public void Execute(DevirtualizationCtx ctx)
        {
            foreach (var virtualizedMethod in ctx.VirtualizedMethods)
            {
                var stream = ctx.Module.Resources.First(q => q.Name == virtualizedMethod.Id);
                if (stream == null)
                {
                    ctx.Logger.Error($"Coulnd't find resource stream {virtualizedMethod.Id}");
                    continue;
                }

                var instructions = stream.GetData();
                if (instructions == null)
                {
                    ctx.Logger.Error($"Resource {virtualizedMethod.Id} has no instructions!");
                    continue;
                }

                instructions = instructions.Select(q => (byte) (q ^ virtualizedMethod.Key)).ToArray();
                var binaryReader = new BinaryReader(new MemoryStream(instructions));
                virtualizedMethod.Instructions = new List<vmInstruction>(binaryReader.ReadInt32());

                for (var i = 0; i < virtualizedMethod.Instructions.Capacity; i++)
                {
                    var instruction = new vmInstruction((vmOpCode) binaryReader.ReadInt32());
                    if (binaryReader.ReadBoolean())
                        switch (binaryReader.ReadInt32())
                        {
                            case 0:
                                instruction.Operand = binaryReader.ReadString();
                                break;
                            case 1:
                                instruction.Operand = binaryReader.ReadInt16();
                                break;
                            case 2:
                                instruction.Operand = binaryReader.ReadInt32();
                                break;
                            case 3:
                                instruction.Operand = binaryReader.ReadInt64();
                                break;
                            case 4:
                                instruction.Operand = binaryReader.ReadUInt16();
                                break;
                            case 5:
                                instruction.Operand = binaryReader.ReadUInt32();
                                break;
                            case 6:
                                instruction.Operand = binaryReader.ReadUInt64();
                                break;
                            case 7:
                                instruction.Operand = binaryReader.ReadDouble();
                                break;
                            case 8:
                                instruction.Operand = binaryReader.ReadDecimal();
                                break;
                            case 9:
                                instruction.Operand = binaryReader.ReadByte();
                                break;
                            case 10:
                                instruction.Operand = binaryReader.ReadSByte();
                                break;
                            case 11:
                                instruction.Operand = binaryReader.ReadSingle();
                                break;
                            case 12:
                                instruction.Operand = null;
                                break;
                        }

                    virtualizedMethod.Instructions.Add(instruction);
                }

                if (virtualizedMethod.Instructions.Count != 0 && ctx.Options.Verbose)
                    ctx.Logger.Success(
                        $"Dissasembled [{virtualizedMethod.Instructions.Count}] VM Instructions on method [{virtualizedMethod.Parent.Name}]");
            }
        }
    }
}