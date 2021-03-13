using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Cmp : AbstractInstruction
    {

        private RegisterOperand _register;
        private AbstractConst _constant;

        public Cmp(RegisterOperand register, AbstractConst constant)
        {
            _register = register;
            _constant = constant;
        }

        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();

            // Add operation byte
            bytecode.Add(Constants.CMP_1632_REGISTER_1632_CONSTANT);

            // Add register bytes as first operand (7 as opcode digit) reference: http://ref.x86asm.net/coder32.html#modrm_byte_32
            bytecode.AddRange(_register.GetBytes(7));

            // Add constant bytes as second operand
            bytecode.AddRange(_constant.GetBytes());
            return bytecode;
        }

    }
}
