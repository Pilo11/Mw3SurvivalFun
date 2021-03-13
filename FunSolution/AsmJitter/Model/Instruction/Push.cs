using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Push : AbstractInstruction
    {

        private Register _register;

        public Push(Register register)
        {
            _register = register;
        }

        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();
            // Add operation byte
            bytecode.Add(Convert.ToByte(Constants.PUSH_1632_REGISTER + _register.Value));
            return bytecode;
        }

    }
}
