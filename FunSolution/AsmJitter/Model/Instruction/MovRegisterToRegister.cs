using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class MovRegisterToRegister : AbstractInstruction
    {

        private RegisterOperand _targetRegister;
        private Register _originRegister;

        public MovRegisterToRegister(RegisterOperand targetRegister, Register originRegister)
        {
            _targetRegister = targetRegister;
            _originRegister = originRegister;
        }

        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();

            // Add operation byte
            bytecode.Add(Constants.MOV_1632_REGISTER_1632_REGISTER);

            // Add register bytes as first operand (origin register as opcode digit) reference: http://ref.x86asm.net/coder32.html#modrm_byte_32
            bytecode.AddRange(_targetRegister.GetBytes((uint)_originRegister.Value));

            return bytecode;
        }

    }
}
