using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Ret : AbstractInstruction
    {
        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();
            // Add operation byte
            bytecode.Add(Constants.RET);
            return bytecode;
        }
    }
}
