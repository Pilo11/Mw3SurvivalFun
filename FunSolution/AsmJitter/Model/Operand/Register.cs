using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Operand
{
    public class Register : RegisterOperand
    {

        public Register(RegisterEnum value) : base(value)
        {
        }

        protected override bool HasMemoryAccess()
        {
            return false;
        }
    }
}
