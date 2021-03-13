using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class MovssRegisterMemoryToRegister : AbstractInstruction
    {

        private Register _targetRegister;
        private RegisterMemory _originRegister;

        public MovssRegisterMemoryToRegister(Register targetRegister, RegisterMemory originRegister)
        {
            _targetRegister = targetRegister;
            _originRegister = originRegister;
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

            // Get the correct opcode and add it
            bytecode.AddRange(_originRegister.GetBytes((uint)_targetRegister.Value));

            return bytecode;
        }

    }
}
