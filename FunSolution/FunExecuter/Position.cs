using System;
using System.Collections.Generic;
using System.Text;

namespace FunExecuter
{
    public class Position
    {

        public Position(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

    }

    public class PositionTeleport
    {

        public Position MinPos { get; }

        public Position MaxPos { get; }

        public Position TargetPos { get; }

        public PositionTeleport(Position minPos, Position maxPos, Position targetPos)
        {
            MinPos = minPos;
            MaxPos = maxPos;
            TargetPos = targetPos;
        }

    }
}
