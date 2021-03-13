using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public abstract class AbstractInstruction
    {

        public abstract IEnumerable<byte> GetBytes();

        public List<byte> InstructionBytes { get; set; } = new List<byte>();

    }
}
