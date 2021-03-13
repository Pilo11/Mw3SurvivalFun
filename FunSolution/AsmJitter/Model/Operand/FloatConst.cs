using AsmJitter.Misc;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Operand
{
    public class FloatConst : AbstractConst
    {

        public float Value { get; }

        public FloatConst(float value)
        {
            Value = value;
        }

        public override IEnumerable<byte> GetBytes()
        {
            return Value.ConvertToByteArray();
        }

    }
}
