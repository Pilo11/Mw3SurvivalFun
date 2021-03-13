using AsmJitter;
using AsmJitter.Model;
using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AsmJitterTest
{
    // Checked with: https://defuse.ca/online-x86-assembler.htm
    public class MovTests
    {
        [Fact]
        public void TestLoad8BitConstIntoRegister()
        {
            var shellcode = new Code();
            shellcode.Mov(new Register(RegisterEnum.ECX_XMM1), new EightBitConstant(2));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0xB9, 0x02, 0x00, 0x00, 0x00
            }, codebytes);
        }

        [Fact]
        public void TestLoadFloatConstIntoRegisterMemory()
        {
            var shellcode = new Code();
            shellcode.Mov(new RegisterMemory(RegisterEnum.EBX_XMM3, 0x1C), new FloatConst(-1024));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0xc7, 0x43, 0x1c, 0x00, 0x00, 0x80, 0xC4
            }, codebytes);
        }

    }
}
