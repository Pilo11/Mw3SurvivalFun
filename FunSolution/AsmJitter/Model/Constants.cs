using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Model
{
    internal class Constants
    {

        public const byte ADDITIONAL_COMMANDS = 0x0F;

        public const byte MOVSS_CONST_XMM_REGISTER_PO = 0x10;

        public const byte SUB_1632_REGISTER_1632_REGISTER = 0x29;

        public const byte COMISS_1632_REGISTER_1632_REGISTER = 0x2F;

        public const byte PUSH_1632_REGISTER = 0x50;

        public const byte POP_1632_REGISTER = 0x58;

        public const byte JB_SHORT = 0x72;

        public const byte JNB_SHORT = 0x73;

        public const byte JE_SHORT = 0x74;

        public const byte JNE_SHORT = 0x75;

        public const byte JL_SHORT = 0x7C;

        public const byte JNL_SHORT = 0x7D;

        public const byte JLE_SHORT = 0x7E;

        public const byte JNLE_SHORT = 0x7F;

        public const byte CMP_1632_REGISTER_1632_CONSTANT = 0x81;

        public const byte MOV_1632_REGISTER_1632_REGISTER = 0x89;

        public const byte NOP = 0x90;

        public const byte MOV_CONST_1632_REGISTER = 0xB8;

        public const byte RET = 0xC3;

        public const byte MOV_CONST_1632_REGISTER_MEMORY = 0xC7;

        public const byte FLOAT_1632_REGISTER_1632_REGISTER = 0xD9;

        public const byte CALL = 0xE8;

        public const byte F3_OP_PREFIX = 0xF3;

    }
}
