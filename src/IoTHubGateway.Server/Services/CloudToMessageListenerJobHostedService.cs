using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<CloudToMessageListenerJobHostedService> _logger;
        private readonly RegisteredDevices registeredDevices;

        public CloudToMessageListenerJobHostedService(IMemoryCache cache, ILogger<CloudToMessageListenerJobHostedService> logger, RegisteredDevices registeredDevices)
        {
            this.cache = cache;
            this._logger = logger;
            this.registeredDevices = registeredDevices;
        }


        private async Task CheckDeviceMessages()
        {
            //Console.WriteLine("Start listening for device messages");

            var deviceIdList = this.registeredDevices.GetDeviceIdList();
            Parallel.ForEach(deviceIdList, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, (deviceId) =>
            {
                try
                {
                    if (this.cache.TryGetValue<DeviceClient>(deviceId, out var deviceClient))
                    {
                        var message = deviceClient.ReceiveAsync(TimeSpan.FromMilliseconds(1)).GetAwaiter().GetResult();
                        if (message != null)
                        {
                            Console.WriteLine($"[{DateTime.Now.ToString()}] Message received");

                            deviceClient.CompleteAsync(message).GetAwaiter().GetResult();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error receiving message from {deviceId}");

                }
            });

            //Console.WriteLine("Ended listening for device messages");

        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($"GracePeriodManagerService is starting.");

            stoppingToken.Register(() =>
                    _logger.LogDebug($" GracePeriod background task is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug($"GracePeriod task doing background work.");

                // This eShopOnContainers method is quering a database table 
                // and publishing events into the Event Bus (RabbitMS / ServiceBus)
                await CheckDeviceMessages();

                //await Task.Delay(1000 * 1, stoppingToken);
            }

            _logger.LogDebug($"GracePeriod background task is stopping.");
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            // Run your graceful clean-up actions
            return Task.CompletedTask;
        }
        
    }
}
