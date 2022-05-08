using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;

namespace BcoreLib
{
    public class BcoreFoundEventArgs : EventArgs
    {
        public BluetoothLEDevice Device;

        public BcoreFoundEventArgs(BluetoothLEDevice device)
        {
            Device = device;
        }
    }

    public enum BcoreConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
    }

    public class BcoreConnectionStatusChangedEventArgs : EventArgs
    {
        public BcoreConnectionStatus Status { get; }

        public BcoreConnectionStatusChangedEventArgs(BcoreConnectionStatus status)
        {
            Status = status;
        }
    }
}
