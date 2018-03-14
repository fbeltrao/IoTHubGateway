using IoTHubGateway.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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

        public GatewayController(IMemoryCache cache, IGatewayService gatewayService)
        {
            this.gatewayService = gatewayService;
        }

        // GET api/values
        [HttpPost("{deviceId}")]
        public async Task<IActionResult> Send(string deviceId, [FromBody] dynamic payload)
        {
            if (string.IsNullOrEmpty(deviceId))
                return BadRequest(new { error = "Missing deviceId" });

            if (payload == null)
                return BadRequest(new { error = "Missing payload" });

            var sasToken = Request.Headers["sas_token"].ToString();

            if (!string.IsNullOrEmpty(sasToken))
            {
                var tokenTTL = Request.Headers["sas_token_ttl"];
                if (!string.IsNullOrEmpty(tokenTTL))
                {

                }
                await gatewayService.SendDeviceToCloudMessageByToken(deviceId, payload.ToString(), sasToken, DateTime.UtcNow.AddMinutes(5));
            }
            else
            {
                await gatewayService.SendDeviceToCloudMessageBySharedAccess(deviceId, payload.ToString());
            }

            return Ok();
        }
    }
}
