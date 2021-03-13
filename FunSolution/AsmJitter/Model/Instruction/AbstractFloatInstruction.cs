using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public abstract class AbstractFloatInstruction : AbstractInstruction
    {

        private RegisterOperand _targetRegister;

        public AbstractFloatInstruction(RegisterOperand targetRegister)
        {
            _targetRegister = targetRegister;
        }

        protected abstract uint GetOpcodeDigit();

        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();

            // Add operation byte
            bytecode.Add(Constants.FLOAT_1632_REGISTER_1632_REGISTER);

            // Add register bytes as first operand (origin register as opcode digit) reference: http://ref.x86asm.net/coder32.html#modrm_byte_32
            bytecode.AddRange(_targetRegister.GetBytes(GetOpcodeDigit()));

            return bytecode;
        }

    }
}
