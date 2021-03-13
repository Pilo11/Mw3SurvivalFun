using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public abstract class AbstractJumpInstruction : AbstractInstruction
    {

        public JumpTarget Target;

        public AbstractJumpInstruction(JumpTarget target)
        {
            Target = target;
        }

        public abstract byte GetOperationByte();

        public override IEnumerable<byte> GetBytes()
        {
            var bytecode = new List<byte>();
            // Add operation byte
            bytecode.Add(GetOperationByte());
            // Dummy byte for a address which will be added later.
            if (Target.TargetAddress is FourBytesConst)
            {
                // Add 4 dummy bytes which represent a 32bit-target address
                bytecode.Add(0x0);
                bytecode.Add(0x0);
                bytecode.Add(0x0);
                bytecode.Add(0x0);
            }
            else
            {
                // Add one dummy byte which represent an 8bit-target address
                bytecode.Add(0x0);
            }
            return bytecode;
        }

    }
}
