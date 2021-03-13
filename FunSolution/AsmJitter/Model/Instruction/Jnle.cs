using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Jnle : AbstractJumpInstruction
    {
        public Jnle(JumpTarget target) : base(target)
        {
        }
        public override byte GetOperationByte()
        {
            return Constants.JNLE_SHORT;
        }
    }
}
