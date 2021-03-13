using AsmJitter.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsmJitter.Model.Operand
{
    public abstract class RegisterOperand : AbstractOperand
    {

        public RegisterEnum Value { get; }

        public RegisterOperand(RegisterEnum value)
        {
            Value = value;
        }       

        public override IEnumerable<byte> GetBytes()
        {
            return Enumerable.Empty<byte>();
        }

        public IEnumerable<byte> GetBytes(uint opcodeDigit)
        {
            var retBytes = new List<byte>();
            // Add opcode field byte
            retBytes.AddRange(GetOpcodeField(opcodeDigit, this));
            // Check if a memory register with an offset was targeted and add the offset bytes if so
            if (HasMemoryAccess())
            {
                var registerMemory = this as RegisterMemory;
                if (registerMemory.Offset != 0)
                {
                    // Check if the offset is 8bit
                    if (registerMemory.Offset.Is8Bit())
                    {
                        // Only add the signed offset byte
                        retBytes.Add(registerMemory.Offset.ConvertToSingleByte());
                    }
                    else
                    {
                        // Add the complete 32bit signed offset
                        retBytes.AddRange(registerMemory.Offset.ConvertToByteArray());
                    }
                }
            }
            return retBytes;
        }

        protected abstract bool HasMemoryAccess();

        private IEnumerable<byte> GetOpcodeField(uint opcodeDigit, RegisterOperand registerOperand)
        {
            if (opcodeDigit > 7)
            {
                throw new ArgumentException($"{nameof(opcodeDigit)} cannot be greater than 7");
            }

            // function result list
            var retValue = new List<byte>();

            // all indeces which are needing additional the SIB byte
            int[] allSibIndeces = new int[] { 4, 12, 20 };
            // 32-bit ModR/M Byte table of http://ref.x86asm.net/coder32.html
            var opcodeFieldMatrix = new byte[][]
            {
                //Digit OpCode 0     1     2     3     4     5     6     7
                new byte[] { 0x00, 0x08, 0x10, 0x18, 0x20, 0x28, 0x30, 0x38 }, // EAX with memory operand
                new byte[] { 0x01, 0x09, 0x11, 0x19, 0x21, 0x29, 0x31, 0x39 }, // ECX with memory operand
                new byte[] { 0x02, 0x0A, 0x12, 0x1A, 0x22, 0x2A, 0x32, 0x3A }, // EDX with memory operand
                new byte[] { 0x03, 0x0B, 0x13, 0x1B, 0x23, 0x2B, 0x33, 0x3B }, // EBX with memory operand
                new byte[] { 0x04, 0x0C, 0x14, 0x1C, 0x24, 0x2C, 0x34, 0x3C }, // sib with memory operand
                new byte[] { 0x05, 0x0D, 0x15, 0x1D, 0x25, 0x2D, 0x35, 0x3D }, // disp32 with memory operand
                new byte[] { 0x06, 0x0E, 0x16, 0x1E, 0x26, 0x2E, 0x36, 0x3E }, // ESI with memory operand
                new byte[] { 0x07, 0x0F, 0x17, 0x1F, 0x27, 0x2F, 0x37, 0x3F }, // EDI with memory operand

                new byte[] { 0x40, 0x48, 0x50, 0x58, 0x60, 0x68, 0x70, 0x78 }, // EAX with memory operand and disp8 offset
                new byte[] { 0x41, 0x49, 0x51, 0x59, 0x61, 0x69, 0x71, 0x79 }, // ECX with memory operand and disp8 offset
                new byte[] { 0x42, 0x4A, 0x52, 0x5A, 0x62, 0x6A, 0x72, 0x7A }, // EDX with memory operand and disp8 offset
                new byte[] { 0x43, 0x4B, 0x53, 0x5B, 0x63, 0x6B, 0x73, 0x7B }, // EBX with memory operand and disp8 offset
                new byte[] { 0x44, 0x4C, 0x54, 0x5C, 0x64, 0x6C, 0x74, 0x7C }, // sib with memory operand and disp8 offset
                new byte[] { 0x45, 0x4D, 0x55, 0x5D, 0x65, 0x6D, 0x75, 0x7D }, // EBP with memory operand and disp8 offset
                new byte[] { 0x46, 0x4E, 0x56, 0x5E, 0x66, 0x6E, 0x76, 0x7E }, // ESI with memory operand and disp8 offset
                new byte[] { 0x47, 0x4F, 0x57, 0x5F, 0x67, 0x6F, 0x77, 0x7F }, // EDI with memory operand and disp8 offset

                new byte[] { 0x80, 0x88, 0x90, 0x98, 0xA0, 0xA8, 0xB0, 0xB8 }, // EAX with memory operand and disp32 offset
                new byte[] { 0x81, 0x89, 0x91, 0x99, 0xA1, 0xA9, 0xB1, 0xB9 }, // ECX with memory operand and disp32 offset
                new byte[] { 0x82, 0x8A, 0x92, 0x9A, 0xA2, 0xAA, 0xB2, 0xBA }, // EDX with memory operand and disp32 offset
                new byte[] { 0x83, 0x8B, 0x93, 0x9B, 0xA3, 0xAB, 0xB3, 0xBB }, // EBX with memory operand and disp32 offset
                new byte[] { 0x84, 0x8C, 0x94, 0x9C, 0xA4, 0xAC, 0xB4, 0xBC }, // sib with memory operand and disp32 offset
                new byte[] { 0x85, 0x8D, 0x95, 0x9D, 0xA5, 0xAD, 0xB5, 0xBD }, // EBP with memory operand and disp32 offset
                new byte[] { 0x86, 0x8E, 0x96, 0x9E, 0xA6, 0xAE, 0xB6, 0xBE }, // ESI with memory operand and disp32 offset
                new byte[] { 0x87, 0x8F, 0x97, 0x9F, 0xA7, 0xAF, 0xB7, 0xBF }, // EDI with memory operand and disp32 offset

                new byte[] { 0xC0, 0xC8, 0xD0, 0xD8, 0xE0, 0xE8, 0xF0, 0xF8 }, // EAX
                new byte[] { 0xC1, 0xC9, 0xD1, 0xD9, 0xE1, 0xE9, 0xF1, 0xF9 }, // ECX
                new byte[] { 0xC2, 0xCA, 0xD2, 0xDA, 0xE2, 0xEA, 0xF2, 0xFA }, // EDX
                new byte[] { 0xC3, 0xCB, 0xD3, 0xDB, 0xE3, 0xEB, 0xF3, 0xFB }, // EBX
                new byte[] { 0xC4, 0xCC, 0xD4, 0xDC, 0xE4, 0xEC, 0xF4, 0xFC }, // ESP
                new byte[] { 0xC5, 0xCD, 0xD5, 0xDD, 0xE5, 0xED, 0xF5, 0xFD }, // EBP
                new byte[] { 0xC6, 0xCE, 0xD6, 0xDE, 0xE6, 0xEE, 0xF6, 0xFE }, // ESI
                new byte[] { 0xC7, 0xCF, 0xD7, 0xDF, 0xE7, 0xEF, 0xF7, 0xFF }, // EDI
            };

            byte opcodeFieldTableOffset = 24; // Set "EAX" line as start point            
            if (HasMemoryAccess())
            {
                var memoryOperand = registerOperand as RegisterMemory;
                opcodeFieldTableOffset = 0; // Set "EAX with memory operand" line as start point
                if (memoryOperand.Offset != 0)
                {
                    if (memoryOperand.Offset.Is8Bit())
                    {
                        opcodeFieldTableOffset += 8; // Set "EAX with memory operand and disp8 offset" line as start point
                    }
                    else
                    {
                        opcodeFieldTableOffset += 16; // Set "EAX with memory operand and disp32 offset" line as start point
                    }
                }                
            }

            int tableAccessIndex = (int)registerOperand.Value + opcodeFieldTableOffset;
            retValue.Add(opcodeFieldMatrix[tableAccessIndex][opcodeDigit]);

            // if a SIB byte is necessary
            if (allSibIndeces.Contains(tableAccessIndex))
            {
                retValue.Add(GetSibByte(registerOperand as RegisterMemory));
            }

            return retValue;
        }

        private byte GetSibByte(RegisterMemory register)
        {
            // 32-bit SIB Byte table of http://ref.x86asm.net/coder32.html
            var sibFieldMatrix = new byte[][]
            {
                //Digit Base:   0(EAX)       1(ECX)      2(EDX)      3(EBX)      4(ESP)      5(NONE)     6(ESI)      7(EDI)
                new byte[]      { 0x00,      0x01,       0x02,       0x03,       0x04,       0x05,       0x06,       0x07 }, // EAX with memory operand
                new byte[]      { 0x08,      0x09,       0x0A,       0x0B,       0x0C,       0x0D,       0x0E,       0x0F }, // ECX with memory operand
                new byte[]      { 0x10,      0x11,       0x12,       0x13,       0x14,       0x15,       0x16,       0x17 }, // EDX with memory operand
                new byte[]      { 0x18,      0x19,       0x1A,       0x1B,       0x1C,       0x1D,       0x1E,       0x1F }, // EBX with memory operand
                new byte[]      { 0x20,      0x21,       0x22,       0x23,       0x24,       0x25,       0x26,       0x27 }, // none
                new byte[]      { 0x28,      0x29,       0x2A,       0x2B,       0x2C,       0x2D,       0x2E,       0x2F }, // EBP with memory operand
                new byte[]      { 0x30,      0x31,       0x32,       0x33,       0x34,       0x35,       0x36,       0x37 }, // ESI with memory operand
                new byte[]      { 0x38,      0x39,       0x3A,       0x3B,       0x3C,       0x3D,       0x3E,       0x3F }, // EDI with memory operand

                new byte[]      { 0x40,      0x41,       0x42,       0x43,       0x44,       0x45,       0x46,       0x47 }, // EAX*2 with memory operand
                new byte[]      { 0x48,      0x49,       0x4A,       0x4B,       0x4C,       0x4D,       0x4E,       0x4F }, // ECX*2 with memory operand
                new byte[]      { 0x50,      0x51,       0x52,       0x53,       0x54,       0x55,       0x56,       0x57 }, // EDX*2 with memory operand
                new byte[]      { 0x58,      0x59,       0x5A,       0x5B,       0x5C,       0x5D,       0x5E,       0x5F }, // EBX*2 with memory operand
                new byte[]      { 0x60,      0x61,       0x62,       0x63,       0x64,       0x65,       0x66,       0x67 }, // none
                new byte[]      { 0x68,      0x69,       0x6A,       0x6B,       0x6C,       0x6D,       0x6E,       0x6F }, // EBP*2 with memory operand
                new byte[]      { 0x70,      0x71,       0x72,       0x73,       0x74,       0x75,       0x76,       0x77 }, // ESI*2 with memory operand
                new byte[]      { 0x78,      0x79,       0x7A,       0x7B,       0x7C,       0x7D,       0x7E,       0x7F }, // EDI*2 with memory operand

                new byte[]      { 0x80,      0x81,       0x82,       0x83,       0x84,       0x85,       0x86,       0x87 }, // EAX*4 with memory operand
                new byte[]      { 0x88,      0x89,       0x8A,       0x8B,       0x8C,       0x8D,       0xBE,       0xBF }, // ECX*4 with memory operand
                new byte[]      { 0x90,      0x91,       0x92,       0x93,       0x94,       0x95,       0x96,       0x97 }, // EDX*4 with memory operand
                new byte[]      { 0x98,      0x99,       0x9A,       0x9B,       0x9C,       0x9D,       0x9E,       0x9F }, // EBX*4 with memory operand
                new byte[]      { 0xA0,      0xA1,       0xA2,       0xA3,       0xA4,       0xA5,       0xA6,       0xA7 }, // none
                new byte[]      { 0xA8,      0xA9,       0xAA,       0xAB,       0xAC,       0xAD,       0xAE,       0xAF }, // EBP*4 with memory operand
                new byte[]      { 0xB0,      0xB1,       0xB2,       0xB3,       0xB4,       0xB5,       0xB6,       0xB7 }, // ESI*4 with memory operand
                new byte[]      { 0xB8,      0xB9,       0xBA,       0xBB,       0xBC,       0xBD,       0xBE,       0xBF }, // EDI*4 with memory operand

                new byte[]      { 0xC0,      0xC1,       0xC2,       0xC3,       0xC4,       0xC5,       0xC6,       0xC7 }, // EAX*8 with memory operand
                new byte[]      { 0xC8,      0xC9,       0xCA,       0xCB,       0xCC,       0xCD,       0xCE,       0xCF }, // ECX*8 with memory operand
                new byte[]      { 0xD0,      0xD1,       0xD2,       0xD3,       0xD4,       0xD5,       0xD6,       0xD7 }, // EDX*8 with memory operand
                new byte[]      { 0xD8,      0xD9,       0xDA,       0xDB,       0xDC,       0xDD,       0xDE,       0xDF }, // EBX*8 with memory operand
                new byte[]      { 0xE0,      0xE1,       0xE2,       0xE3,       0xE4,       0xE5,       0xE6,       0xE7 }, // none
                new byte[]      { 0xE8,      0xE9,       0xEA,       0xEB,       0xEC,       0xED,       0xEE,       0xEF }, // EBP*8 with memory operand
                new byte[]      { 0xF0,      0xF1,       0xF2,       0xF3,       0xF4,       0xF5,       0xF6,       0xF7 }, // ESI*8 with memory operand
                new byte[]      { 0xF8,      0xF9,       0xFA,       0xFB,       0xFC,       0xFD,       0xFE,       0xFF }, // EDI*8 with memory operand
            };

            int tableAccessIndex = (int)register.Value;

            // TODO: Replace the hardcoded constant with a correct index calculation
            return sibFieldMatrix[4][tableAccessIndex];
        }

    }
}
