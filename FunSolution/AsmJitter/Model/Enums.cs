using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model
{
    /// <summary>
    /// Reference: http://ref.x86asm.net/coder32.html#modrm_byte_32
    /// </summary>
    public enum RegisterEnum
    {
        EAX_XMM0 = 0,
        ECX_XMM1 = 1,
        EDX_XMM2 = 2,
        EBX_XMM3 = 3,
        ESP_SIB_XMM4 = 4, // ESP or SIB byte, ref: http://ref.x86asm.net/coder32.html#sib_byte_32
        EBP_DS32_XMM5 = 5,
        ESI_XMM6 = 6,
        EDI_XMM7 = 7
    }
}
