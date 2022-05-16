using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;

namespace BcoreLib
{
    /// <summary>
    /// bCore発見イベント値
    /// </summary>
    public class BcoreFoundEventArgs : EventArgs
    {
        /// <summary>
        /// bCoreデバイス
        /// </summary>
        public string Name { get; }
        public string Id { get; }

        public BcoreFoundEventArgs(BluetoothLEDevice device)
        {
            Name = device.Name;
            Id = device.DeviceId;
        }
    }

    /// <summary>
    /// bCore接続状態
    /// </summary>
    public enum BcoreConnectionStatus
    {
        /// <summary>
        /// 切断
        /// </summary>
        Disconnected,
        /// <summary>
        /// 接続中
        /// </summary>
        Connecting,
        /// <summary>
        /// 接続済み
        /// </summary>
        Connected,
        /// <summary>
        /// 切断厨
        /// </summary>
        Disconnecting,
    }

    /// <summary>
    /// bCore接続状態更新イベント値
    /// </summary>
    public class BcoreConnectionStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// bCore接続状態
        /// </summary>
        public BcoreConnectionStatus Status { get; }

        public BcoreConnectionStatusChangedEventArgs(BcoreConnectionStatus status)
        {
            Status = status;
        }
    }
}
