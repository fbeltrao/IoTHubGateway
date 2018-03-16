using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTHubGateway.Server.Services
{
    /// <summary>
    /// Maintains a list of registered devices to enabled cloud messages background job to query them
    /// </summary>
    public class RegisteredDevices
    {
        System.Collections.Concurrent.ConcurrentDictionary<string, string> devices = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        public void AddDevice(string deviceId)
        {
            devices.AddOrUpdate(deviceId, deviceId, (key, existing) =>
            {
                return deviceId;
            });
        }

        public void RemoveDevice(object key)
        {
            devices.Remove(key.ToString(), out var value);
        }

        public ICollection<string> GetDeviceIdList()
        {
            return this.devices.Keys;
        }
    }
}
