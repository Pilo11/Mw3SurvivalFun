using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Je : AbstractJumpInstruction
    {
        public Je(JumpTarget target) : base(target)
        {
        }
        public override byte GetOperationByte()
        {
            return Constants.JE_SHORT;
        }
    }
}
