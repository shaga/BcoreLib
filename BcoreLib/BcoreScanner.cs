using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace BcoreLib
{
    public class BcoreScanner
    {
        #region const

        private static readonly BluetoothLEAdvertisement BcoreAdvertisement = new BluetoothLEAdvertisement()
        {
            ServiceUuids = { BcoreUuids.BcoreService }
        };

        private static readonly BluetoothLEAdvertisementFilter BcoreAdvertisementFilter =
            new BluetoothLEAdvertisementFilter()
            {
                Advertisement = BcoreAdvertisement
            };

        #endregion

        #region field

        private readonly BluetoothLEAdvertisementWatcher _watcher;

        #endregion

        #region prorerty

        public BluetoothLEAdvertisementWatcherStatus ScanStatus =>
            _watcher?.Status ?? BluetoothLEAdvertisementWatcherStatus.Stopped;

        public bool IsScanning => _watcher?.Status == BluetoothLEAdvertisementWatcherStatus.Started;

        #endregion

        #region event

        public event EventHandler<BcoreFoundEventArgs> FoundDevice;

        #endregion

        public BcoreScanner()
        {
            _watcher = new BluetoothLEAdvertisementWatcher()
            {
                AdvertisementFilter = BcoreAdvertisementFilter,
                ScanningMode = BluetoothLEScanningMode.Active,
                
            };
            _watcher.Received += OnWatcherReceived;
        }

        #region method

        public void Start()
        {
            if (IsScanning) return;

            _watcher.Start();
        }

        public void Stop()
        {
            if (!IsScanning) return;

            _watcher.Stop();
        }

        private async void OnWatcherReceived(BluetoothLEAdvertisementWatcher watcher,
            BluetoothLEAdvertisementReceivedEventArgs e)
        {
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(e.BluetoothAddress);

            FoundDevice?.Invoke(this, new BcoreFoundEventArgs(device));
        }

        #endregion
    }
}
