using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Pop : AbstractInstruction
    {

        private Register _register;

        public Pop(Register register)
        {
            _register = register;
        }

        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();
            // Add operation byte
            bytecode.Add(Convert.ToByte(Constants.POP_1632_REGISTER + _register.Value));
            return bytecode;
        }

    }
}
