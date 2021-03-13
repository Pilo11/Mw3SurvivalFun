using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Sub : AbstractInstruction
    {

        private RegisterOperand _targetRegister;
        private Register _subtrahendRegister;

        public Sub(RegisterOperand targetRegister, Register subtrahendRegister)
        {
            _targetRegister = targetRegister;
            _subtrahendRegister = subtrahendRegister;
        }

        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();

            // Add operation byte
            bytecode.Add(Constants.SUB_1632_REGISTER_1632_REGISTER);

            // Add register bytes as first operand (subtrahend as opcode digit) reference: http://ref.x86asm.net/coder32.html#modrm_byte_32
            bytecode.AddRange(_targetRegister.GetBytes((uint)_subtrahendRegister.Value));

            return bytecode;
        }

    }
}
