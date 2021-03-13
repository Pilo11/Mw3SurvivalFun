using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Nop : AbstractInstruction
    {
        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();
            // Add operation byte
            bytecode.Add(Constants.NOP);
            return bytecode;
        }
    }
}
