using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTHubGateway.Server
{
    /// <summary>
    /// Defines the server options
    /// </summary>
    public class ServerOptions
    {
        public int DeviceOperationTimeoutInMilliseconds { get; set; } =  1000 * 10;

        /// <summary>
        /// IoT Hub host name. Something like xxxxx.azure-devices.net
        /// </summary>
        public string IoTHubHostName { get; set; }

        /// <summary>
        /// The IoT Hub access policy name. A common value is iothubowner
        /// </summary>
        public string AccessPolicyName { get; set; }

        /// <summary>
        /// The IoT Hub access policy key. Get it from Azure Portal
        /// </summary>
        public string AccessPolicyKey { get; set; }

        /// <summary>
        /// The maximum pool size (default is <seealso cref="ushort.MaxValue"/>)
        /// </summary>
        public int MaxPoolSize { get; set; } = ushort.MaxValue;

        /// <summary>
        /// Allows or not the usage of shared access policy keys
        /// If you enable it make sure that access to the API is protected otherwise anyone will be able to impersonate devices
        /// </summary>
        public bool SharedAccessPolicyKeyEnabled { get; set; }

        /// <summary>
        /// Default device client cache in duration in minutes
        /// 60 minutes by default
        /// </summary>
        public int DefaultDeviceCacheInMinutes = 60;

    }
}
