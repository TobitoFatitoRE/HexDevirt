namespace HexDevirt.Core
{
    public class vmInstruction
    {
        public vmInstruction(vmOpCode opCode, object operand = null)
        {
            OpCode = opCode;
            Operand = operand;
        }

        public vmOpCode OpCode { get; set; }
        public object Operand { get; set; }
    }
}