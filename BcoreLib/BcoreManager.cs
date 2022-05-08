using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace BcoreLib
{
    public class BcoreManager : IDisposable
    {
        #region const



        #endregion

        #region field

        private BluetoothLEDevice _device;

        private Dictionary<Guid, GattDeviceService> _bcoreServices;

        private Dictionary<Guid, GattCharacteristic> _bcoreCharacteristics;

        private readonly bool[] _portOutState;

        #endregion

        #region property

        public bool IsInitialized { get; private set; }

        #endregion

        #region event

        public event EventHandler<BcoreConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        #endregion

        #region constructor

        public BcoreManager(BluetoothLEDevice device)
        {
            _device = device;
            _device.ConnectionStatusChanged += OnConnectionChanged;

            _bcoreServices = new Dictionary<Guid, GattDeviceService>();
            _bcoreCharacteristics = new Dictionary<Guid, GattCharacteristic>();

            _portOutState = new bool[Bcore.MaxFunctionCount];
        }

        #endregion

        #region method

        public void Dispose()
        {
            foreach (var service in _bcoreServices.Values)
            {
                service.Dispose();
            }

            _bcoreCharacteristics.Clear();
            _bcoreServices.Clear();
            _device?.Dispose();
            _device = null;
            GC.Collect();
        }

        public async Task Init()
        {
            RaiseConnectionChanged(BcoreConnectionStatus.Connecting);
            if (_device == null)
            {
                RaiseConnectionChanged(BcoreConnectionStatus.Disconnected);
                return;
            }

            var services = await _device.GetGattServicesAsync();

            if (services == null || services.Services.Count == 0)
            {
                RaiseConnectionChanged(BcoreConnectionStatus.Disconnected);
                return;
            }

            foreach (var service in services.Services)
            {
                _bcoreServices.Add(service.Uuid, service);
            }

            foreach (var service in _bcoreServices.Values)
            {
                var characteristics = await service.GetCharacteristicsAsync();

                foreach (var characteristic in characteristics.Characteristics)
                {
                    _bcoreCharacteristics.Add(characteristic.Uuid, characteristic);
                }
            }
        }

        public async Task<int> ReadBattery()
        {
            var value = await ReadValue(BcoreUuids.BatteryCharacteristic);

            if (value == null) return 0;

            return value[0] | (value[1] << 8);
        }

        public async Task<BcoreFunctionInfo> ReadFunctionInfo()
        {
            var value = await ReadValue(BcoreUuids.FunctionCharacteristic);

            if (value == null) return null;

            return new BcoreFunctionInfo(value);
        }

        public async Task WriteMotorPwmAsync(int idx, int speed, bool isFlip = false)
        {
            if (idx < 0 || Bcore.MaxFunctionCount <= idx) return;

            await WriteValue(BcoreUuids.MotorCharacteristic, Bcore.CreateMotorSpeedValue(idx, speed, isFlip));
        }

        public async void WriteMotorPwm(int idx, int speed, bool isFlip = false)
        {
            await WriteMotorPwmAsync(idx, speed, isFlip);
        }

        public async Task WriteServoPosAsync(int idx, int pos, bool isFlip = false, int trim = 0)
        {
            if (idx < 0 || Bcore.MaxFunctionCount <= idx) return;

            await WriteValue(BcoreUuids.ServoCharacteristic, Bcore.CreateServoPosValue(idx, pos, isFlip, trim));
        }

        public async Task WriteServoPosAsync(int idx, int pos, int trim)
        {
            await WriteServoPosAsync(idx, pos, false, trim);
        }

        public async void WriteServoPos(int idx, int pos, bool isFlip = false, int trim = 0)
        {
            await WriteServoPosAsync(idx, pos, isFlip, trim);
        }

        public async Task WriteServoPos(int idx, int pos, int trim)
        {
            await WriteServoPosAsync(idx, pos, trim);
        }

        public async Task WritePortOutAsync(int idx, bool isOn)
        {
            if (idx < 0 || Bcore.MaxFunctionCount <= idx) return;

            _portOutState[idx] = isOn;

            await WriteValue(BcoreUuids.PortOutCharacteristic, Bcore.CreatePortOutValue(_portOutState));
        }

        public async Task WritePortOutAsync(bool[] state)
        {
            for (var i = 0; i < Bcore.MaxFunctionCount; i++)
            {
                if (i < state.Length)
                {
                    _portOutState[i] = state[i];
                }
                else
                {
                    _portOutState[i] = false;
                }
            }
            await WriteValue(BcoreUuids.PortOutCharacteristic, Bcore.CreatePortOutValue(state));
        }

        public async void WritePortOut(int idx, bool isOn)
        {
            await WritePortOutAsync(idx, isOn);
        }

        public async void WritePortOut(bool[] state)
        {
            await WritePortOutAsync(state);
        }

        private void OnConnectionChanged(BluetoothLEDevice device, object e)
        {
            var connected = device.ConnectionStatus == BluetoothConnectionStatus.Connected;
            RaiseConnectionChanged(connected ? BcoreConnectionStatus.Connected : BcoreConnectionStatus.Disconnected);
        }

        private void RaiseConnectionChanged(BcoreConnectionStatus status)
        {
            ConnectionStatusChanged?.Invoke(this, new BcoreConnectionStatusChangedEventArgs(status));
        }

        private async Task WriteValue(Guid uuid, byte[] data)
        {
            if (!_bcoreCharacteristics.ContainsKey(uuid)) return;

            var characteristic = _bcoreCharacteristics[uuid];

            if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
            {
                return;
            }

            await characteristic.WriteValueAsync(data.AsBuffer(), GattWriteOption.WriteWithoutResponse);
        }

        private async Task<byte[]> ReadValue(Guid uuid)
        {
            if (!_bcoreCharacteristics.ContainsKey(uuid)) return null;

            var characteristic = _bcoreCharacteristics[uuid];

            if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
            {
                return null;
            }

            var result = await characteristic.ReadValueAsync();

            return result.Value.ToArray();
        }

        #endregion
    }
}
