using AsmJitter.Misc;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Operand
{
    public class FourBytesConst : AbstractConst
    {

        public int Value { get; }

        public FourBytesConst(int value)
        {
            if (value.Is8Bit())
            {
                throw new ArgumentException($"The given value: {value} is a 8bit constant. Please use the 8bit const class.");
            }
            Value = value;
        }

        public override IEnumerable<byte> GetBytes()
        {
            return Value.ConvertToByteArray();
        }

    }
}
