using Microsoft.Azure.Devices.Client;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTHubGateway.Server.Services
{
    /// <summary>
    /// Defines a connected device, wrapping a <see cref="DeviceClient"/>
    /// </summary>
    public class ConnectedDevice
    {
        private readonly string iotHubName;
        private readonly string deviceId;
        private readonly string uniqueId;
        private readonly DeviceClient deviceClient;
        CircuitBreakerPolicy breaker;
        //static CircuitBreakerPolicy breaker = Policy.Handle<Exception>().CircuitBreakerAsync(1, TimeSpan.FromMinutes(1));

        /// <summary>
        /// Time to wait once a receive message fails before trying again
        /// </summary>
        public TimeSpan ReceiveMessageOnErrorBreakDuration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="deviceClient"></param>
        public ConnectedDevice(string iotHubName, string deviceId, DeviceClient deviceClient)
        {
            this.iotHubName = iotHubName;
            this.deviceId = deviceId;
            this.uniqueId = Utils.UniqueDeviceID(iotHubName, deviceId);
            this.deviceClient = deviceClient;

            

            this.breaker = Policy.Handle<Exception>().CircuitBreakerAsync(1, this.ReceiveMessageOnErrorBreakDuration);
        }

        /// <summary>
        /// Sends device to cloud message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendEventAsync(Message message) => await this.deviceClient.SendEventAsync(message);


        /// <summary>
        /// Receives and forwards cloud message with a circuit breaker
        /// If the operation fails it will wait a certain amount of time before retrying
        /// If the circuit breaker is open (meaning it is not even trying) the exception <see cref="BrokenCircuitException"/>
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <param name="cloudMessageCallback"></param>
        /// <returns></returns>
        public async Task ReceiveAndForwardCloudMessage(TimeSpan timeSpan, Action<string, string, Message> cloudMessageCallback)
        {
            try
            {
                await breaker.ExecuteAsync(async () => await InternalReceiveAndForwardCloudMessage(timeSpan, cloudMessageCallback));
            }
            catch (BrokenCircuitException)
            {
                // ignore error since the circuit is open
                // other errors will bubble up
            }
        }

        async Task InternalReceiveAndForwardCloudMessage(TimeSpan timeSpan, Action<string, string, Message> cloudMessageCallback)
        {
            var message = await deviceClient.ReceiveAsync(TimeSpan.FromMilliseconds(1));
            if (message != null)
            {
                try
                {
                    cloudMessageCallback(this.iotHubName ?? string.Empty, this.deviceId, message);

                    await deviceClient.CompleteAsync(message);
                }
                catch (Exception ex)
                {
                    await deviceClient.AbandonAsync(message);
                    throw new Exception($"Error handling message from {deviceId}", ex);
                }
            }
        }
    }
}
