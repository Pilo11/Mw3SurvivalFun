using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Operand
{
    public class EightBitConstant : AbstractConst
    {

        public sbyte Value { get; }

        public EightBitConstant(sbyte value)
        {
            Value = value;
        }

        public override IEnumerable<byte> GetBytes()
        {
            return new List<byte>()
            {
                (byte)Value
            };
        }

    }
}
