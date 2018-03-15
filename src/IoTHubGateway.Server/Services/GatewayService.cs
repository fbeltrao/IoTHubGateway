
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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
        private readonly ServerOptions gatewayOptions;
        private readonly IMemoryCache cache;

        /// <summary>
        /// Sliding expiration for each device client connection
        /// Default: 30 minutes
        /// </summary>
        public TimeSpan DeviceConnectionCacheSlidingExpiration { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public GatewayService(IOptions<ServerOptions> serverOptions, IMemoryCache cache)
        {
            this.gatewayOptions = serverOptions.Value;
            this.cache = cache;
            this.DeviceConnectionCacheSlidingExpiration = TimeSpan.FromMinutes(serverOptions.Value.DefaultDeviceCacheInMinutes);
        }

        public async Task SendDeviceToCloudMessageByToken(string deviceId, string payload, string sasToken, DateTime tokenExpiration)
        {
            var deviceClient = await ResolveDeviceClient(deviceId, sasToken, tokenExpiration);

            await deviceClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes(payload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json"
            });
        }

        public async Task SendDeviceToCloudMessageBySharedAccess(string deviceId, string payload)
        {
            var deviceClient = await ResolveDeviceClient(deviceId);

            await deviceClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes(payload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json"
            });
        }

        private async Task<DeviceClient> ResolveDeviceClient(string deviceId, string sasToken = null, DateTime? tokenExpiration = null)
        {
            var deviceClient = await cache.GetOrCreateAsync<DeviceClient>(deviceId, async(cacheEntry) =>
            {
                IAuthenticationMethod auth = null;
                if (string.IsNullOrEmpty(sasToken))
                {
                    auth = new DeviceAuthenticationWithSharedAccessPolicyKey(deviceId, this.gatewayOptions.AccessPolicyName, this.gatewayOptions.AccessPolicyKey);
                }
                else
                {
                    auth = new DeviceAuthenticationWithToken(deviceId, sasToken);
                }
                
                var newDeviceClient = DeviceClient.Create(
                   this.gatewayOptions.IoTHubHostName,
                    auth,
                    new ITransportSettings[]
                    {
                        new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                        {
                            AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                            {
                                Pooling = true,
                                MaxPoolSize = (uint)this.gatewayOptions.MaxPoolSize,
                            }
                        }
                    }
                );

                newDeviceClient.OperationTimeoutInMilliseconds = (uint)this.gatewayOptions.DeviceOperationTimeoutInMilliseconds;

                await newDeviceClient.OpenAsync();

                if (!tokenExpiration.HasValue)
                    tokenExpiration = DateTime.UtcNow.AddMinutes(this.gatewayOptions.DefaultDeviceCacheInMinutes);
                cacheEntry.SetAbsoluteExpiration(tokenExpiration.Value);

                return newDeviceClient;
            });


            return deviceClient;
        }
    }
}