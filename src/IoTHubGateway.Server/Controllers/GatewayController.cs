using IoTHubGateway.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTHubGateway.Server.Controllers
{
    [Route("api")]
    public class GatewayController : Controller
    {
        private readonly IGatewayService gatewayService;
        private readonly ServerOptions options;
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public GatewayController(IGatewayService gatewayService, IOptions<ServerOptions> options)
        {
            this.gatewayService = gatewayService;
            this.options = options.Value;

#if DEBUG
            if (this.options.DirectMethodEnabled && this.options.DirectMethodCallback == null)
            {
                this.options.DirectMethodCallback = (methodRequest, userContext) =>
                {
                    var deviceId = (string)userContext;
                    Console.WriteLine($"Device method call: {deviceId}.{methodRequest.Name}({methodRequest.DataAsJson})");

                    var responseBody = "{ succeeded: true }";
                    MethodResponse methodResponse = new MethodResponse(Encoding.UTF8.GetBytes(responseBody), 200);

                    return Task.FromResult(methodResponse);
                };
            }
#endif
        }

        // GET api/values
        [HttpPost("{deviceId}")]
        public async Task<IActionResult> Send(string deviceId, [FromBody] dynamic payload)
        {
            if (string.IsNullOrEmpty(deviceId))
                return BadRequest(new { error = "Missing deviceId" });

            if (payload == null)
                return BadRequest(new { error = "Missing payload" });

            
            var sasToken = this.ControllerContext.HttpContext.Request.Headers[Constants.SasTokenHeaderName].ToString();

            if (!string.IsNullOrEmpty(sasToken))
            {
                var tokenExpirationDate = ResolveTokenExpiration(sasToken);
                if (!tokenExpirationDate.HasValue)
                    tokenExpirationDate = DateTime.UtcNow.AddMinutes(20);

                await gatewayService.SendDeviceToCloudMessageByToken(deviceId, payload.ToString(), sasToken, tokenExpirationDate.Value);
            }
            else
            {
                if (!this.options.SharedAccessPolicyKeyEnabled)
                    return BadRequest(new { error = "Shared access is not enabled" });
                await gatewayService.SendDeviceToCloudMessageBySharedAccess(deviceId, payload.ToString());
            }

            return Ok();
        }

        private DateTime? ResolveTokenExpiration(string sasToken)
        {
            const string field = "se=";
            var index = sasToken.LastIndexOf(field);
            if (index >= 0)
            {
                var unixTime = sasToken.Substring(index + field.Length);
                if (int.TryParse(unixTime, out var unixTimeInt))
                {
                    return epoch.AddSeconds(unixTimeInt);
                }
            }

            return null;
        }
    }
}
