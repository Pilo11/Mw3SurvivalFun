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
    public class CompareTests
    {
        [Fact]
        public void TestCmpFromConstToRegister()
        {
            var shellcode = new Code();
            shellcode.Cmp(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(0x5140));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0x81, 0xF9, 0x40, 0x51, 0x00, 0x00
            }, codebytes);
        }
        
        [Fact]
        public void TestCmpFromConstToRegisterMemory()
        {
            var shellcode = new Code();
            shellcode.Cmp(new RegisterMemory(RegisterEnum.ECX_XMM1), new FourBytesConst(0x5140));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0x81, 0x39, 0x40, 0x51, 0x00, 0x00
            }, codebytes);
        }

        [Fact]
        public void TestCmpFromConstToRegisterMemoryWithLittleOffset()
        {
            var shellcode = new Code();
            shellcode.Cmp(new RegisterMemory(RegisterEnum.ECX_XMM1, 0x1), new FourBytesConst(0x5140));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0x81, 0x79, 0x01, 0x40, 0x51, 0x00, 0x00
            }, codebytes);
        }

        [Fact]
        public void TestCmpFromConstToRegisterMemoryWithLittleNegativeDutyOffset()
        {
            var shellcode = new Code();
            shellcode.Cmp(new RegisterMemory(RegisterEnum.ECX_XMM1, -0x80), new FourBytesConst(0x5140));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0x81, 0x79, 0x80, 0x40, 0x51, 0x00, 0x00
            }, codebytes);
        }

        [Fact]
        public void TestCmpFromConstToRegisterMemoryWithLittleNegativeOffset()
        {
            var shellcode = new Code();
            shellcode.Cmp(new RegisterMemory(RegisterEnum.ECX_XMM1, -0x7F), new FourBytesConst(0x5140));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0x81, 0x79, 0x81, 0x40, 0x51, 0x00, 0x00
            }, codebytes);
        }

        [Fact]
        public void TestCmpFromConstToRegisterMemoryWithBigOffset()
        {
            var shellcode = new Code();
            shellcode.Cmp(new RegisterMemory(RegisterEnum.ECX_XMM1, 0x150), new FourBytesConst(0x5140));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0x81, 0xb9, 0x50, 0x01, 0x00, 0x00, 0x40, 0x51, 0x00, 0x00
            }, codebytes);
        }

        [Fact]
        public void TestComissFromRegisterToRegister()
        {
            var shellcode = new Code();
            shellcode.Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0x0F, 0x2F, 0xC1
            }, codebytes);
        }

    }
}
