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
    public class ConstantTests
    {
        [Fact]
        public void TestFloatConst()
        {
            var floatConst = new FloatConst(0.5f);
            Assert.Equal(new byte[]
            {
                0x00, 0x00, 0x00, 0x3F
            }, floatConst.GetBytes());
        }

        [Fact]
        public void TestNegativeFloatConst()
        {
            var floatConst = new FloatConst(-221.57567f);
            Assert.Equal(new byte[]
            {
                0x5F, 0x93, 0x5D, 0xC3
            }, floatConst.GetBytes());
        }

    }
}
