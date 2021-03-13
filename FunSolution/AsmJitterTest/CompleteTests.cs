using AsmJitter;
using AsmJitter.Model;
using AsmJitter.Model.Instruction;
using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using Xunit;

namespace AsmJitterTest
{
    public class CompleteTests
    {
        
        [Fact]
        public void TestCompleteSomeOperations1()
        {
            var shellcode = new Code();

            var returnLabel = new Label("return", shellcode);

            shellcode.Cmp(new RegisterMemory(RegisterEnum.ESI_XMM6, 0x150), new FourBytesConst(0x5140))
                .Je(returnLabel)
                .Sub(new Register(RegisterEnum.ECX_XMM1), new Register(RegisterEnum.EBP_DS32_XMM5))
                .Mov(new RegisterMemory(RegisterEnum.ESI_XMM6, 0x150), new Register(RegisterEnum.ECX_XMM1))
                .Label(returnLabel)
                .Ret();

            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0x81, 0xBE, 0x50, 0x01, 0x00, 0x00, 0x40, 0x51, 0x00, 0x00, 0x74, 0x08, 0x29, 0xE9, 0x89, 0x8E, 0x50, 0x01, 0x00, 0x00, 0xC3
            }, codebytes);
        }

    }
}
