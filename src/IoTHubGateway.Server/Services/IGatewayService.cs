using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTHubGateway.Server.Services
{
    /// <summary>
    /// Gateway to IoT Hub service
    /// </summary>
    public interface IGatewayService
    {
        /// <summary>
        /// Sends device to cloud message using device token
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="payload"></param>
        /// <param name="sasToken"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        Task SendDeviceToCloudMessageByToken(string deviceId, string payload, string sasToken, DateTime dateTime);

        /// <summary>
        /// Sends device to cloud message using shared access token
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        Task SendDeviceToCloudMessageBySharedAccess(string deviceId, string payload);
    }
}
