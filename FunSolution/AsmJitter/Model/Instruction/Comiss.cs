using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Comiss : AbstractInstruction
    {

        private Register _firstRegister;
        private Register _secondRegister;

        public Comiss(Register firstRegister, Register secondRegister)
        {
            _firstRegister = firstRegister;
            _secondRegister = secondRegister;
        }

        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();

            // Add additional commands prefix byte
            bytecode.Add(Constants.ADDITIONAL_COMMANDS);

            // Add operation byte
            bytecode.Add(Constants.COMISS_1632_REGISTER_1632_REGISTER);

            // Add register value as opcode digit
            bytecode.AddRange(_secondRegister.GetBytes((uint)_firstRegister.Value));

            return bytecode;
        }

    }
}
