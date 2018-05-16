
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace IoTHubGateway.Server.Services
{
    /// <summary>
    /// IoT Hub Device client multiplexer based on AMQP
    /// </summary>
    public class GatewayService : IGatewayService
    {
        private readonly ServerOptions serverOptions;
        private readonly IMemoryCache cache;
        private readonly ILogger<GatewayService> logger;
        RegisteredDevices registeredDevices;

        /// <summary>
        /// Sliding expiration for each device client connection
        /// Default: 30 minutes
        /// </summary>
        public TimeSpan DeviceConnectionCacheSlidingExpiration { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public GatewayService(ServerOptions serverOptions, IMemoryCache cache, ILogger<GatewayService> logger, RegisteredDevices registeredDevices)
        {
            this.serverOptions = serverOptions;
            this.cache = cache;
            this.logger = logger;
            this.registeredDevices = registeredDevices;
            this.DeviceConnectionCacheSlidingExpiration = TimeSpan.FromMinutes(serverOptions.DefaultDeviceCacheInMinutes);
        }


        /// <inheritdoc />
        public async Task SendDeviceToCloudMessageByConnectionString(string connectionString, string deviceId, string payload)
        {
            var deviceClient = await ResolveDeviceClient(connectionString: connectionString, deviceId: deviceId);
            if (deviceClient == null)
                throw new DeviceConnectionException($"Failed to connect to device {deviceId}");

            await InternalSendDeviceMessage(deviceClient, deviceId, payload);
        }

        /// <inheritdoc />
        public async Task SendDeviceToCloudMessageByToken(string deviceId, string payload, string sasToken, DateTime tokenExpiration)
        {
            var deviceClient = await ResolveDeviceClient(deviceId, sasToken, tokenExpiration);
            if (deviceClient == null)
                throw new DeviceConnectionException($"Failed to connect to device {deviceId}");

            await InternalSendDeviceMessage(deviceClient, deviceId, payload);

        }

        /// <inheritdoc />
        public async Task SendDeviceToCloudMessageBySharedAccess(string deviceId, string payload)
        {
            var deviceClient = await ResolveDeviceClient(deviceId: deviceId);
            if (deviceClient == null)
                throw new DeviceConnectionException($"Failed to connect to device {deviceId}");

            await InternalSendDeviceMessage(deviceClient, deviceId, payload);
        }

        async Task InternalSendDeviceMessage(ConnectedDevice deviceClient, string deviceId, string payload)
        {
            try
            {
                await deviceClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes(payload))
                {
                    ContentEncoding = "utf-8",
                    ContentType = "application/json"
                });

                this.logger.LogInformation($"Event sent to device {deviceId} using device token. Payload: {payload}");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Could not send device message to IoT Hub (device: {deviceId})");
                throw;
            }
        }

        /// <summary>
        /// Resolves a <see cref="ConnectedDevice"/> by looking the cache and then creating a new one
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="sasToken"></param>
        /// <param name="tokenExpiration"></param>
        /// <returns></returns>
        private async Task<ConnectedDevice> ResolveDeviceClient(string deviceId, string sasToken = null, DateTime? tokenExpiration = null) => await ResolveDeviceClient(null, deviceId, sasToken, tokenExpiration);

        /// <summary>
        /// Resolves a <see cref="ConnectedDevice"/> by looking the cache and then creating a new one
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="deviceId"></param>
        /// <param name="sasToken"></param>
        /// <param name="tokenExpiration"></param>
        /// <returns></returns>
        private async Task<ConnectedDevice> ResolveDeviceClient(string connectionString, string deviceId, string sasToken = null, DateTime? tokenExpiration = null)
        {
            try
            {
                
                var iotHubName = string.Empty;
                if (!string.IsNullOrEmpty(connectionString))
                    iotHubName = Utils.ResolveIoTHubHostName(connectionString);
                
                var uniqueDeviceId = Utils.UniqueDeviceID(iotHubName, deviceId);
                var deviceClient = await cache.GetOrCreateAsync<ConnectedDevice>(uniqueDeviceId, async (cacheEntry) =>
                {
                    DeviceClient newDeviceClient = null;
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        IAuthenticationMethod auth = null;
                        if (string.IsNullOrEmpty(sasToken))
                        {
                            auth = new DeviceAuthenticationWithSharedAccessPolicyKey(deviceId, this.serverOptions.AccessPolicyName, this.serverOptions.AccessPolicyKey);
                        }
                        else
                        {
                            auth = new DeviceAuthenticationWithToken(deviceId, sasToken);
                        }

                        newDeviceClient = DeviceClient.Create(
                            this.serverOptions.IoTHubHostName,
                            auth,
                            new ITransportSettings[]
                            {
                                new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                                {
                                    AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                                    {
                                        Pooling = true,
                                        MaxPoolSize = (uint)this.serverOptions.MaxPoolSize,
                                    }
                                }
                            }
                        );
                    }
                    else
                    {
                        newDeviceClient = DeviceClient.CreateFromConnectionString(
                            connectionString, 
                            deviceId,
                            new ITransportSettings[]
                            {
                                new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                                {
                                    AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                                    {
                                        Pooling = true,
                                        MaxPoolSize = (uint)this.serverOptions.MaxPoolSize,
                                    }
                                }
                            });
                    }

                    newDeviceClient.OperationTimeoutInMilliseconds = (uint)this.serverOptions.DeviceOperationTimeout;

                    await newDeviceClient.OpenAsync();

                    if (this.serverOptions.DirectMethodEnabled)
                    {
                        // when connecting with multiple tenants this method might fail, use polly to retry
                        var retryPolicy = Policy
                            .Handle<Exception>()
                            .WaitAndRetryAsync(4, (attempt) => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                        await retryPolicy.ExecuteAsync(async () => await newDeviceClient.SetMethodDefaultHandlerAsync(this.serverOptions.DirectMethodCallback, deviceId));
                    }

                    if (!tokenExpiration.HasValue)
                        tokenExpiration = DateTime.UtcNow.AddMinutes(this.serverOptions.DefaultDeviceCacheInMinutes);
                    cacheEntry.SetAbsoluteExpiration(tokenExpiration.Value);
                    cacheEntry.RegisterPostEvictionCallback(this.CacheEntryRemoved, deviceId);

                    this.logger.LogInformation($"Connection to device {deviceId} in {iotHubName ?? "predefined iot hub"} has been established, valid until {tokenExpiration.Value.ToString()}");

                    
                    registeredDevices.AddDevice(uniqueDeviceId);

                    return new ConnectedDevice(iotHubName, deviceId, newDeviceClient);
                });


                return deviceClient;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Could not connect device {deviceId}");
            }

            return null;
        }

        private void CacheEntryRemoved(object key, object value, EvictionReason reason, object state)
        {
            this.registeredDevices.RemoveDevice(key);
        }
    }
}