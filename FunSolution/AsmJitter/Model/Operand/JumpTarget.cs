using AsmJitter.Model.Instruction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsmJitter.Model.Operand
{
    public class JumpTarget : AbstractOperand
    {

        public FourBytesConst TargetAddress { get; }

        public Label Label { get; }

        public JumpTarget(FourBytesConst targetAddress)
        {
            TargetAddress = targetAddress;
        }

        public JumpTarget(Label label)
        {
            Label = label;
        }

        /// <summary>
        /// Cannot define the target bytes yet...
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<byte> GetBytes()
        {
            return Enumerable.Empty<byte>();
        }

    }
}
