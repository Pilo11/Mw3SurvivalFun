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
    public class FloatRegisterTests
    {
        [Fact]
        public void TestRegisterLoadWithMemoryRegisterAndOffset()
        {
            var shellcode = new Code();
            shellcode.Fld(new RegisterMemory(RegisterEnum.ESP_SIB_XMM4, 0x4C));
            var codebytes = shellcode.GetBytes();
            Assert.Equal(new byte[]
            {
                0xD9, 0x44, 0x24, 0x4C
            }, codebytes);
        }
        
    }
}
