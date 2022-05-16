using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace BcoreLib
{
    /// <summary>
    /// bCore操作
    /// </summary>
    public class BcoreManager : IDisposable
    {
        #region field

        private string _deviceId;

        private BluetoothLEDevice _device;

        private GattDeviceService _bcoreService;

        private Dictionary<Guid, GattDeviceService> _bcoreServices;

        private Dictionary<Guid, GattCharacteristic> _bcoreCharacteristics;

        private readonly bool[] _portOutState;

        #endregion

        #region property

        /// <summary>
        /// 初期化済みフラグ
        /// </summary>
        public bool IsInitialized => (_device?.ConnectionStatus ?? BluetoothConnectionStatus.Disconnected) ==
                                     BluetoothConnectionStatus.Connected;

        public string DeviceName => _device?.Name;

        #endregion

        #region event

        /// <summary>
        /// 接続状態更新イベント
        /// </summary>
        public event EventHandler<BcoreConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        #endregion

        #region constructor

        public BcoreManager(string deviceId)
        {
            _deviceId = deviceId;
            //_device.ConnectionStatusChanged += OnConnectionChanged;

            _bcoreServices = new Dictionary<Guid, GattDeviceService>();
            _bcoreCharacteristics = new Dictionary<Guid, GattCharacteristic>();

            _portOutState = new bool[Bcore.MaxFunctionCount];
        }

        #endregion

        #region method

        /// <summary>
        /// 廃棄
        /// </summary>
        public void Dispose()
        {
            Final();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <returns></returns>
        public async Task Init()
        {
            RaiseConnectionChanged(BcoreConnectionStatus.Connecting);
            if (_deviceId == null)
            {
                RaiseConnectionChanged(BcoreConnectionStatus.Disconnected);
                return;
            }

            _device = await BluetoothLEDevice.FromIdAsync(_deviceId);

            if (_device == null)
            {
                RaiseConnectionChanged(BcoreConnectionStatus.Disconnected);
                return;
            }

            var session = await GattSession.FromDeviceIdAsync(_device.BluetoothDeviceId);
            session.MaintainConnection = false;

            _device.ConnectionStatusChanged += OnConnectionChanged;

            var services = await _device.GetGattServicesAsync(BluetoothCacheMode.Cached);

            if (services == null || services.Services.Count == 0)
            {
                RaiseConnectionChanged(BcoreConnectionStatus.Disconnected);
                return;
            }

            foreach (var service in services.Services)
            {
                Debug.WriteLine(service.Uuid);
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

        /// <summary>
        /// 終了処理
        /// </summary>
        public void Final()
        {
            RaiseConnectionChanged(BcoreConnectionStatus.Disconnecting);

            foreach (var uuid in _bcoreServices.Keys.Reverse())
            {
                var service = _bcoreServices[uuid];
                if (service.Session.SessionStatus == GattSessionStatus.Active)
                {
                    service.Session.Dispose();
                }
                _bcoreServices[uuid].Dispose();
                _bcoreServices[uuid] = null;
            }

            _bcoreCharacteristics.Clear();
            _bcoreServices.Clear();
            _device.ConnectionStatusChanged -= OnConnectionChanged;
            _device?.Dispose();
            _device = null;
            GC.Collect();
        }

        /// <summary>
        /// バッテリ値読み込み
        /// </summary>
        /// <returns>バッテリ値(mV)</returns>
        public async Task<int> ReadBattery()
        {
            var value = await ReadValue(BcoreUuids.BatteryCharacteristic);

            if (value == null) return 0;

            return value[0] | (value[1] << 8);
        }

        /// <summary>
        /// 機能譲歩読み込み
        /// </summary>
        /// <returns>bCore機能情報</returns>
        public async Task<BcoreFunctionInfo> ReadFunctionInfo()
        {
            var value = await ReadValue(BcoreUuids.FunctionCharacteristic);

            if (value == null) return null;

            return new BcoreFunctionInfo(value);
        }

        /// <summary>
        /// モータPWM書き込み
        /// </summary>
        /// <param name="idx">モータINDEX</param>
        /// <param name="speed">モータ速度</param>
        /// <param name="isFlip">反転フラグ</param>
        /// <returns></returns>
        public async Task WriteMotorPwmAsync(int idx, int speed, bool isFlip = false)
        {
            if (idx < 0 || Bcore.MaxFunctionCount <= idx) return;

            await WriteValue(BcoreUuids.MotorCharacteristic, Bcore.CreateMotorSpeedValue(idx, speed, isFlip));
        }

        /// <summary>
        /// モータPWM書き込み(完了待ちしない)
        /// </summary>
        /// <param name="idx">モータINDEXモータ</param>
        /// <param name="speed">モータ速度</param>
        /// <param name="isFlip">反転フラグ</param>
        public async void WriteMotorPwm(int idx, int speed, bool isFlip = false)
        {
            await WriteMotorPwmAsync(idx, speed, isFlip);
        }

        /// <summary>
        /// サーボ位置書き込み
        /// </summary>
        /// <param name="idx">サーボINDEX</param>
        /// <param name="pos">サーボ位置</param>
        /// <param name="isFlip">反転フラグ</param>
        /// <param name="trim">トリム値</param>
        /// <returns></returns>
        public async Task WriteServoPosAsync(int idx, int pos, bool isFlip = false, int trim = 0)
        {
            if (idx < 0 || Bcore.MaxFunctionCount <= idx) return;

            await WriteValue(BcoreUuids.ServoCharacteristic, Bcore.CreateServoPosValue(idx, pos, isFlip, trim));
        }

        /// <summary>
        /// サーボ位置書き込み
        /// </summary>
        /// <param name="idx">サーボINDEX</param>
        /// <param name="pos">サーボ位置</param>
        /// <param name="trim">トリム値</param>
        /// <returns></returns>
        public async Task WriteServoPosAsync(int idx, int pos, int trim)
        {
            await WriteServoPosAsync(idx, pos, false, trim);
        }

        /// <summary>
        /// サーボ位置書き込み(完了待ちしない
        /// </summary>
        /// <param name="idx">サーボINDEX</param>
        /// <param name="pos">サーボ位置</param>
        /// <param name="isFlip">反転フラグ</param>
        /// <param name="trim">トリム値</param>
        public async void WriteServoPos(int idx, int pos, bool isFlip = false, int trim = 0)
        {
            await WriteServoPosAsync(idx, pos, isFlip, trim);
        }

        /// <summary>
        /// サーボ位置書き込み(完了待ちしない
        /// </summary>
        /// <param name="idx">サーボINDEX</param>
        /// <param name="pos">サーボ位置</param>
        /// <param name="trim">トリム値</param>
        public async void WriteServoPos(int idx, int pos, int trim)
        {
            await WriteServoPosAsync(idx, pos, trim);
        }

        /// <summary>
        /// ポートアウト書き込み
        /// </summary>
        /// <param name="idx">ポートINDEX</param>
        /// <param name="isOn">true=ON/false=OFF</param>
        /// <returns></returns>
        public async Task WritePortOutAsync(int idx, bool isOn)
        {
            if (idx < 0 || Bcore.MaxFunctionCount <= idx) return;

            _portOutState[idx] = isOn;

            await WriteValue(BcoreUuids.PortOutCharacteristic, Bcore.CreatePortOutValue(_portOutState));
        }

        /// <summary>
        /// ポートアウト書き込み
        /// </summary>
        /// <param name="state">各ポート状態</param>
        /// <returns></returns>
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

        /// <summary>
        /// ポートアウト書き込み(完了待ちしない
        /// </summary>
        /// <param name="idx">ポートINDEX</param>
        /// <param name="isOn">true=ON/false=OFF</param>
        public async void WritePortOut(int idx, bool isOn)
        {
            await WritePortOutAsync(idx, isOn);
        }

        /// <summary>
        /// ポートアウト書き込み(完了待ちしない
        /// </summary>
        /// <param name="state">各ポート状態</param>
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
