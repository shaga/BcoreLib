using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BcoreLib
{
    public static class Bcore
    {
        #region const

        public const int MaxFunctionCount = 4;

        public const int MinMotorPwm = 0x00;
        public const int MaxMotorPwm = 0xff;
        public const int StopMotorPwm = 0x80;

        public const int MinServoPos = 0x00;
        public const int MaxServoPos = 0xff;
        public const int CenterServoPos = 0x80;

        #endregion

        public static byte[] CreateMotorSpeedValue(int idx, int speed, bool isFlip = false)
        {
            if (isFlip) speed = MaxMotorPwm - speed;

            if (speed > MaxMotorPwm) speed = MaxMotorPwm;
            else if (speed < MinServoPos) speed = MinServoPos;

            return new[] {(byte) idx, (byte) (speed & 0xff)};
        }

        public static byte[] CreateServoPosValue(int idx, int pos, bool isFlip = false, int trim = 0)
        {
            pos += trim;

            if (isFlip) pos = MaxServoPos - pos;

            if (pos > MaxServoPos) pos = MaxServoPos;
            else if (pos < MinServoPos) pos = MinServoPos;

            return new[] {(byte) idx, (byte) (pos & 0xff)};
        }

        public static byte[] CreateServoPosValue(int idx, int pos, int trim)
        {
            return CreateServoPosValue(idx, pos, false, trim);
        }

        public static byte[] CreatePortOutValue(bool[] state)
        {
            var value = 0;

            for (var i = 0; i < MaxFunctionCount && i < state.Length; i++)
            {
                if (!state[i]) continue;

                value |= 0x01 << i;
            }

            return new[] {(byte) value};
        }
    }
}
