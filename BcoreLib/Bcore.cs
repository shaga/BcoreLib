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

        /// <summary>
        /// ポート最大数
        /// </summary>
        public const int MaxFunctionCount = 4;

        /// <summary>
        /// モータPWM最小値
        /// </summary>
        public const int MinMotorPwm = 0x00;

        /// <summary>
        /// モータPWM最大値
        /// </summary>
        public const int MaxMotorPwm = 0xff;

        /// <summary>
        /// モータPWM停止値
        /// </summary>
        public const int StopMotorPwm = 0x80;

        /// <summary>
        /// サーボ位置最小値
        /// </summary>
        public const int MinServoPos = 0x00;

        /// <summary>
        /// サーボ位置最大値
        /// </summary>
        public const int MaxServoPos = 0xff;

        /// <summary>
        /// サーボ位置中央値
        /// </summary>
        public const int CenterServoPos = 0x80;

        #endregion

        /// <summary>
        /// モータ速度送信値生成
        /// </summary>
        /// <param name="idx">モータINDEX</param>
        /// <param name="speed">モータスピード</param>
        /// <param name="isFlip">反転フラグa</param>
        /// <returns>bCore送信データ</returns>
        public static byte[] CreateMotorSpeedValue(int idx, int speed, bool isFlip = false)
        {
            if (isFlip) speed = MaxMotorPwm - speed;

            if (speed > MaxMotorPwm) speed = MaxMotorPwm;
            else if (speed < MinServoPos) speed = MinServoPos;

            return new[] {(byte) idx, (byte) (speed & 0xff)};
        }

        /// <summary>
        /// サーボ位置送信値生成
        /// </summary>
        /// <param name="idx">サーボINDEX</param>
        /// <param name="pos">サーボ位置</param>
        /// <param name="isFlip">反転フラグ</param>
        /// <param name="trim">トリム値</param>
        /// <returns>bCore送信データ</returns>
        public static byte[] CreateServoPosValue(int idx, int pos, bool isFlip = false, int trim = 0)
        {
            pos += trim;

            if (isFlip) pos = MaxServoPos - pos;

            if (pos > MaxServoPos) pos = MaxServoPos;
            else if (pos < MinServoPos) pos = MinServoPos;

            return new[] {(byte) idx, (byte) (pos & 0xff)};
        }

        /// <summary>
        /// サーボ位置送信値生成
        /// </summary>
        /// <param name="idx">サーボINDEX</param>
        /// <param name="pos">サーボ位置</param>
        /// <param name="trim">トリム値</param>
        /// <returns>bCore送信データ</returns>
        public static byte[] CreateServoPosValue(int idx, int pos, int trim)
        {
            return CreateServoPosValue(idx, pos, false, trim);
        }

        /// <summary>
        /// ポートアウト送信値生成
        /// </summary>
        /// <param name="state">ポート状態</param>
        /// <returns>bCore送信データ</returns>
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
