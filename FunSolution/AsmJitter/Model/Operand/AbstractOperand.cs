using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Operand
{
    public abstract class AbstractOperand
    {

        public abstract IEnumerable<byte> GetBytes();

    }
}
