using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Fld : AbstractFloatInstruction
    {

        public Fld(RegisterOperand targetRegister) : base(targetRegister)
        {
        }

        // opcode digit reference: http://ref.x86asm.net/coder32.html
        protected override uint GetOpcodeDigit()
        {
            return 0;
        }

    }
}
