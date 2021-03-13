using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class MovssConstantMemoryToRegister : AbstractInstruction
    {

        private Register _targetRegister;
        private FourBytesConst _originConstant;

        public MovssConstantMemoryToRegister(Register targetRegister, FourBytesConst originConstant)
        {
            _targetRegister = targetRegister;
            _originConstant = originConstant;
        }

        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();

            // Add prefix byte
            bytecode.Add(Constants.F3_OP_PREFIX);

            // Add operation byte
            bytecode.Add(Constants.ADDITIONAL_COMMANDS);

            // Add primary opcode byte
            bytecode.Add(Constants.MOVSS_CONST_XMM_REGISTER_PO);

            // Create a pseudo registerMemory for the 32bit DS (data segment), get the correct opcode and add it
            bytecode.AddRange(new RegisterMemory(RegisterEnum.EBP_DS32_XMM5).GetBytes((uint)_targetRegister.Value));

            // Add constant bytes
            bytecode.AddRange(_originConstant.GetBytes());

            return bytecode;
        }

    }
}
