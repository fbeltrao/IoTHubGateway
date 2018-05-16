using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTHubGateway.Server
{
    public static class Utils
    {
        /// <summary>
        /// Creates unique device identifier for a iot hub and device id
        /// </summary>
        /// <param name="iotHubHostName"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static string UniqueDeviceID(string iotHubHostName, string deviceId)
        {
            return string.IsNullOrEmpty(iotHubHostName) ?
                deviceId :
                string.Concat(iotHubHostName, "_", deviceId);
        }

        /// <summary>
        /// Tries to resolve the iot hub host name from the connection string
        /// </summary>
        /// <param name="iothubConnectionString"></param>
        /// <returns></returns>
        public static string ResolveIoTHubHostName(string iothubConnectionString)
        {
            var connectionStringBuilder = new System.Data.Common.DbConnectionStringBuilder();
            connectionStringBuilder.ConnectionString = iothubConnectionString;

            if (connectionStringBuilder.TryGetValue("hostname", out var iotHubHostName))
                return iotHubHostName.ToString().ToLowerInvariant().Replace(".azure-devices.net", string.Empty);

            return string.Empty;

        }
    }
}
