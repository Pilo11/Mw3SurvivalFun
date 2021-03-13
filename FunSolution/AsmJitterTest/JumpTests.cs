using AsmJitter;
using AsmJitter.Model;
using AsmJitter.Model.Instruction;
using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AsmJitterTest
{
    // Checked with: https://defuse.ca/online-x86-assembler.htm
    public class JumpTests
    {        

        [Fact]
        public void TestSimpleUpJumpWithLabel()
        {
            var shellcode = new Code();
            var label1 = new Label("label1", shellcode);
            shellcode.Nop()
                .Label(label1)
                .Nop()
                .Je(label1);
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0x90, 0x90, 0x74, 0xFD
            }, codebytes);
        }

        [Fact]
        public void TestSimpleDownJumpWithLabel()
        {
            var shellcode = new Code();
            var label1 = new Label("label1", shellcode);
            shellcode.Nop()
                .Je(label1)
                .Nop()
                .Nop()
                .Label(label1)
                .Nop();
                
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0x90, 0x74, 0x02, 0x90, 0x90, 0x90
            }, codebytes);
        }

    }
}
