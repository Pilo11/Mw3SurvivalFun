using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Jnb : AbstractJumpInstruction
    {
        public Jnb(JumpTarget target) : base(target)
        {
        }
        public override byte GetOperationByte()
        {
            return Constants.JNB_SHORT;
        }
    }
}
