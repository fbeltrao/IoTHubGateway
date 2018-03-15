using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTHubGateway.Server.Services
{
    public class RegisteredDevices
    {
        System.Collections.Concurrent.ConcurrentDictionary<string, string> devices = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        internal void RegisterDevice(string deviceId)
        {
            devices.AddOrUpdate(deviceId, deviceId, (key, existing) =>
            {
                return deviceId;
            });
        }

        internal void DeviceRemoved(object key)
        {
            devices.Remove(key.ToString(), out var value);
        }

        internal ICollection<string> GetDeviceIdList()
        {
            return this.devices.Keys;
        }
    }
}
