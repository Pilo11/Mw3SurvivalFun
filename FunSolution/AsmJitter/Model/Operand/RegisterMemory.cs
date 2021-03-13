using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Operand
{
    public class RegisterMemory : RegisterOperand
    {

        public int Offset { get; }

        public RegisterMemory(RegisterEnum value, int offset = 0) : base(value)
        {
            Offset = offset;
        }
        protected override bool HasMemoryAccess()
        {
            return true;
        }
    }
}
