using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{    
    public class Jl : AbstractJumpInstruction
    {
        public Jl(JumpTarget target) : base(target)
        {
        }
        public override byte GetOperationByte()
        {
            return Constants.JL_SHORT;
        }
    }
}
