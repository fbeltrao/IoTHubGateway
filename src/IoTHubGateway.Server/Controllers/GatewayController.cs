using IoTHubGateway.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
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
                var tokenExpiratinDate = DateTime.UtcNow.AddMinutes(5);

                var tokenExpiration = Request.Headers[Constants.SasTokenExpirationHeaderName];
                if (!string.IsNullOrEmpty(tokenExpiration))
                {
                    if (!long.TryParse(tokenExpiration, out var tokenExpirationEpoch))
                        return BadRequest(new { error = "Invalid sas_token_expiration" });

                    var givenTokenExpirationDate = epoch.AddSeconds(tokenExpirationEpoch);
                    if (givenTokenExpirationDate < DateTime.UtcNow)
                        return BadRequest(new { error = "Provided token already expired" });

                    tokenExpiratinDate = givenTokenExpirationDate;

                }
                await gatewayService.SendDeviceToCloudMessageByToken(deviceId, payload.ToString(), sasToken, tokenExpiratinDate);
            }
            else
            {
                if (!this.options.SharedAccessPolicyKeyEnabled)
                    return BadRequest(new { error = "Shared access is not enabled" });
                await gatewayService.SendDeviceToCloudMessageBySharedAccess(deviceId, payload.ToString());
            }

            return Ok();
        }
    }
}
