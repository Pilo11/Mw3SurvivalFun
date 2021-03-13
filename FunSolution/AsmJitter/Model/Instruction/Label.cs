using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsmJitter.Model.Instruction
{
    public class Label : AbstractInstruction
    {

        public string Name { get; }

        public AbstractInstruction NextInstruction { get; set; }

        public Label(string name, Code code)
        {
            Name = name;
            code.AddLabel(this);
        }

        public override IEnumerable<byte> GetBytes()
        {
            return Enumerable.Empty<byte>();
        }

    }
}
