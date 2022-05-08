using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BcoreLib
{
    /// <summary>
    /// bCore機能情報
    /// </summary>
    public class BcoreFunctionInfo
    {
        #region const

        private const int IdxMotor = 0;
        private const int IdxServoPortOut = 1;

        private const int OffsetMotor = 0;
        private const int OffsetServo = 0;
        private const int OffsetPortOut = 4;

        #endregion

        #region field

        private int _motorPorts;

        private int _servoPorts;

        private int _portOuts;

        #endregion

        #region property

        /// <summary>
        /// モータ数
        /// </summary>
        public int MotorCount => GetPortCount(_motorPorts);

        /// <summary>
        /// サーボ数
        /// </summary>
        public int ServoCount => GetPortCount(_servoPorts);

        /// <summary>
        /// ポートアウト数
        /// </summary>
        public int PortOutCount => GetPortCount(_portOuts);

        #endregion

        #region constructor

        public BcoreFunctionInfo(byte[] source)
        {
            SetPortInfo(ref _motorPorts, source, IdxMotor, OffsetMotor);
            SetPortInfo(ref _servoPorts, source, IdxServoPortOut, OffsetServo);
            SetPortInfo(ref _portOuts, source, IdxServoPortOut, OffsetPortOut);
        }

        #endregion

        #region method

        /// <summary>
        /// モータ状態
        /// </summary>
        /// <param name="idx">モータINDEX</param>
        /// <returns>true=有効/false=無効</returns>
        public bool IsEnableMotorPort(int idx)
        {
            return IsEnablePort(idx, _motorPorts);
        }

        /// <summary>
        /// サーボ状態
        /// </summary>
        /// <param name="idx">サーボINDEX</param>
        /// <returns>true=有効/false=無効</returns>
        public bool IsEnableServoPort(int idx)
        {
            return IsEnablePort(idx, _servoPorts);
        }

        /// <summary>
        /// ポートアウト状態
        /// </summary>
        /// <param name="idx">ポートアウトINDEX</param>
        /// <returns>true=有効/false=無効</returns>
        public bool IsEnablePortOut(int idx)
        {
            return IsEnablePort(idx, _portOuts);
        }

        private void SetPortInfo(ref int value, byte[] source, int index, int offset)
        {
            value = index < source.Length ? (source[index] >> offset) & 0x0f : 0;
        }

        private bool IsEnablePort(int idx, int source)
        {
            if (idx < 0 || Bcore.MaxFunctionCount <= idx) return false;

            return ((source >> idx) & 0x01) == 0x01;
        }

        private int GetPortCount(int source)
        {
            return Enumerable.Range(0, Bcore.MaxFunctionCount).Count(idx => IsEnablePort(idx, source));
        }

        #endregion
    }
}
