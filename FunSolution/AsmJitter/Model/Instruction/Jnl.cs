using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Jnl : AbstractJumpInstruction
    {
        public Jnl(JumpTarget target) : base(target)
        {
        }
        public override byte GetOperationByte()
        {
            return Constants.JNL_SHORT;
        }
    }
}
