using System.Collections.Generic;
using AsmResolver.DotNet;

namespace HexDevirt.Core
{
    public class VirtualizedMethod
    {
        public VirtualizedMethod(MethodDefinition parent, string Id, int Key)
        {
            Parent = parent;
            this.Id = Id;
            this.Key = Key;
        }

        public MethodDefinition Parent { get; set; }
        public string Id { get; set; }
        public int Key { get; set; }
        public List<vmInstruction> Instructions { get; set; }
    }
}