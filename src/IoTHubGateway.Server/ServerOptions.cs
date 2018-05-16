using Microsoft.Azure.Devices.Client;
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

        /// <summary>
        /// Device operation timeout (in milliseconds)
        /// Default: 10000 (10 seconds)
        /// </summary>
        public int DeviceOperationTimeout { get; set; } =  1000 * 10;

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
        /// Allows or not the usage of the device connection string being passed in the request header
        /// Callers will need to know the destination IoT Hub connection string for the device
        /// </summary>
        public bool DeviceConnectionStringEnabled { get; set; }

        /// <summary>
        /// Default device client cache in duration in minutes
        /// 60 minutes by default
        /// </summary>
        public int DefaultDeviceCacheInMinutes = 60;

        /// <summary>
        /// Enable/disables direct method (cloud -> device)
        /// </summary>
        public bool DirectMethodEnabled { get; set; } = false;

        /// <summary>
        /// Gets/sets the callback to handle device direct methods
        /// </summary>
        public MethodCallback DirectMethodCallback { get; set; }        

        /// <summary>
        /// Enable/disables cloud messages in the gateway
        /// Cloud messages are retrieved in a background job
        /// Default: false / disabled
        /// </summary>
        public bool CloudMessagesEnabled { get; set; }

        /// <summary>
        /// Degree of parallelism used to check for cloud messages
        /// </summary>
        public int CloudMessageParallelism { get; set; } = 10;


        /// <summary>
        /// Gets/sets the callback to handle cloud messages
        /// </summary>
        public Action<string, string, Message> CloudMessageCallback { get; set; }
        
    }
}
