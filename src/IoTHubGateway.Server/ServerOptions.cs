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
        /// The access policy name. A common value is iothubowner
        /// </summary>
        public string AccessPolicyName { get; set; }

        /// <summary>
        /// The access policy key. Get it from Azure Portal
        /// </summary>
        public string AccessPolicyKey { get; set; }

        /// <summary>
        /// The maximum pool size (default = 1). Recommended is 1 per ~995 devices
        /// </summary>
        public int MaxPoolSize { get; set; } = 1;

        /// <summary>
        /// Default device client cache in duration in minutes
        /// </summary>
        public int DefaultDeviceCacheInMinutes = 30;

    }
}
