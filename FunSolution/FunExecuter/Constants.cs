using System;
using System.Collections.Generic;
using System.Text;

namespace FunExecuter
{
    internal class Constants
    {

        // Addresses
        internal const int HEALTH_SUBSTRACTION_INSTRUCTION = 0x0045FB98;
        internal const int POSITION_X_FLOATLOAD_INSTRUCTION = 0x00535CD5;
        internal const int HOSTPLAYER_ID = 0x1197AD8;
        internal const int SECONDPLAYER_ID = 0x1197D48;

        internal const int HOST_PLAYER_POSITION_BASE = 0x01381D48;
        internal const int HOST_PLAYER_POSITION_X = 0x01381D64;
        internal const int HOST_PLAYER_POSITION_Y = 0x01381D68;
        internal const int HOST_PLAYER_POSITION_Z = 0x01381D6C;

        internal const int SECOND_PLAYER_POSITION_X = 0x0138CE28;
        internal const int SECOND_PLAYER_POSITION_Y = 0x0138CE2C;
        internal const int SECOND_PLAYER_POSITION_Z = 0x0138CE30;

        internal const int BULLET_REDUCTION_CODE = 0x00463FA2;

        // Values
        internal const int HEALTH_SENTRY_VALUE = 0x5140;

    }
}
