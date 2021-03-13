using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class MovConstantToRegister : AbstractInstruction
    {

        private Register _targetRegister;
        private AbstractConst _originConstant;

        public MovConstantToRegister(Register targetRegister, AbstractConst originConstant)
        {
            _targetRegister = targetRegister;
            _originConstant = originConstant;
        }

        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();

            // Add operation byte
            bytecode.Add(Convert.ToByte(Constants.MOV_CONST_1632_REGISTER + _targetRegister.Value));

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
