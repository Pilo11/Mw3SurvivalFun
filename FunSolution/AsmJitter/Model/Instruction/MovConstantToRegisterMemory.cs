using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class MovConstantToRegisterMemory : AbstractInstruction
    {

        private RegisterMemory _targetRegister;
        private AbstractConst _originConstant;

        public MovConstantToRegisterMemory(RegisterMemory targetRegister, AbstractConst originConstant)
        {
            _targetRegister = targetRegister;
            _originConstant = originConstant;
        }

        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();

            // Add operation byte
            bytecode.Add(Constants.MOV_CONST_1632_REGISTER_MEMORY);

            // Add register bytes as first operand, reference: http://ref.x86asm.net/coder32.html#modrm_byte_32
            bytecode.AddRange(_targetRegister.GetBytes(0));

            // Add constant bytes
            bytecode.AddRange(_originConstant.GetBytes());

            // If the constant is an 8bit const add three zero bytes
            if (_originConstant is EightBitConstant)
            {
                bytecode.Add(0);
                bytecode.Add(0);
                bytecode.Add(0);
            }

            return bytecode;
        }

    }
}
