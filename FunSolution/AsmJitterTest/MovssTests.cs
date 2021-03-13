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
    public class MovssTests
    {
        [Fact]
        public void TestLoad32bitMemoryValueIntoXMM0Register()
        {
            var shellcode = new Code();
            shellcode.Movss(new Register(RegisterEnum.EAX_XMM0), new FourBytesConst(0x419b5c));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0xF3, 0x0F, 0x10, 0x05, 0x5C, 0x9B, 0x41, 0x00
            }, codebytes);
        }

        [Fact]
        public void TestLoad32bitMemoryValueIntoXMM1Register()
        {
            var shellcode = new Code();
            shellcode.Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(0x419b5c));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0xF3, 0x0F, 0x10, 0x0D, 0x5C, 0x9B, 0x41, 0x00
            }, codebytes);
        }

        [Fact]
        public void TestLoadRegisterMemoryWithOffsetValueIntoXMM0Register()
        {
            var shellcode = new Code();
            shellcode.Movss(new Register(RegisterEnum.EAX_XMM0), new RegisterMemory(RegisterEnum.EBX_XMM3, 0x1C));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0xF3, 0x0F, 0x10, 0x43, 0x1C
            }, codebytes);
        }

    }
}
