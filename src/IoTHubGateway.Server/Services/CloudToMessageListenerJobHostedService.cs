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
    /// <summary>
    /// Background job listening for cloud messages to <see cref="ConnectedDevice"/>
    /// </summary>
    public class CloudToMessageListenerJobHostedService : IHostedService
    {
        private readonly IMemoryCache cache;
        private readonly ILogger<CloudToMessageListenerJobHostedService> logger;
        private readonly RegisteredDevices registeredDevices;
        private readonly ServerOptions serverOptions;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="logger"></param>
        /// <param name="registeredDevices"></param>
        /// <param name="serverOptions"></param>
        public CloudToMessageListenerJobHostedService(
            IMemoryCache cache, 
            ILogger<CloudToMessageListenerJobHostedService> logger, 
            RegisteredDevices registeredDevices,
            ServerOptions serverOptions)
        {
            this.cache = cache;
            this.logger = logger;
            this.registeredDevices = registeredDevices;
            this.serverOptions = serverOptions;
        }


        /// <summary>
        /// Checks for device messages
        /// </summary>
        /// <returns></returns>
        private void CheckDeviceMessages(CancellationToken stoppingToken)
        {
            var deviceIdList = this.registeredDevices.GetDeviceIdList();
            Parallel.ForEach(deviceIdList, new ParallelOptions() { MaxDegreeOfParallelism = serverOptions.CloudMessageParallelism }, (deviceId) =>
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    if (this.cache.TryGetValue<ConnectedDevice>(deviceId, out var connectedDevice))
                    {
                        try
                        {
                            connectedDevice
                                .ReceiveAndForwardCloudMessage(TimeSpan.FromMilliseconds(1), this.serverOptions.CloudMessageCallback)
                                .GetAwaiter()
                                .GetResult();
                        }
                        catch (Exception handlingMessageException)
                        {
                            logger.LogError(handlingMessageException, $"Error handling message from {deviceId}");
                        }
                    }
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

            if (this.serverOptions.CloudMessageCallback == null)
            {
                logger.LogInformation($"{nameof(CloudToMessageListenerJobHostedService)} not executing as no handler was defined in {nameof(ServerOptions)}.{nameof(ServerOptions.CloudMessageCallback)}.");
            }
            else
            {
                stoppingToken.Register(() => logger.LogDebug($" {nameof(CloudToMessageListenerJobHostedService)} background task is stopping."));

                while (!stoppingToken.IsCancellationRequested)
                {
                    CheckDeviceMessages(stoppingToken);
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
