using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IoTHubGateway.Server.Services
{
    public class CloudToMessageListenerJobHostedService : IHostedService
    {
        private readonly IMemoryCache cache;
        private readonly ILogger<CloudToMessageListenerJobHostedService> logger;
        private readonly RegisteredDevices registeredDevices;
        private readonly ServerOptions serverOptions;

        public CloudToMessageListenerJobHostedService(
            IMemoryCache cache, 
            ILogger<CloudToMessageListenerJobHostedService> logger, 
            RegisteredDevices registeredDevices,
            IOptions<ServerOptions> serverOptions)
        {
            this.cache = cache;
            this.logger = logger;
            this.registeredDevices = registeredDevices;
            this.serverOptions = serverOptions.Value;
        }


        /// <summary>
        /// Checks for device messages
        /// </summary>
        /// <returns></returns>
        private void CheckDeviceMessages()
        {
            var deviceIdList = this.registeredDevices.GetDeviceIdList();
            Parallel.ForEach(deviceIdList, new ParallelOptions() { MaxDegreeOfParallelism = serverOptions.CloudMessageParallelism }, (deviceId) =>
            {
                try
                {
                    if (this.cache.TryGetValue<DeviceClient>(deviceId, out var deviceClient))
                    {
                        var message = deviceClient.ReceiveAsync(TimeSpan.FromMilliseconds(1)).GetAwaiter().GetResult();
                        if (message != null)
                        {
                            try
                            {                                
                                //this.serverOptions.CloudMessageHandler(deviceId, message);
                                // Console.WriteLine($"[{DateTime.Now.ToString()}] Message received");

                                deviceClient.CompleteAsync(message).GetAwaiter().GetResult();
                            }
                            catch (Exception handlingMessageException)
                            {
                                logger.LogError(handlingMessageException, $"Error handling message from {deviceId}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error receiving message from {deviceId}");
                }
            });
        }

        /// <summary>
        /// Starts job
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken stoppingToken)
        {
            logger.LogDebug($"{nameof(CloudToMessageListenerJobHostedService)} is starting.");

            if (this.serverOptions.CloudMessageHandler == null)
            {
                logger.LogInformation($"{nameof(CloudToMessageListenerJobHostedService)} not executing as no handler was defined in {nameof(ServerOptions)}.{nameof(ServerOptions.CloudMessageHandler)}.");
            }
            else
            {
                stoppingToken.Register(() => logger.LogDebug($" {nameof(CloudToMessageListenerJobHostedService)} background task is stopping."));

                while (!stoppingToken.IsCancellationRequested)
                {
                    CheckDeviceMessages();
                }
            }

            logger.LogDebug($"{nameof(CloudToMessageListenerJobHostedService)} background task is stopping.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Ends the job
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
        
    }
}
