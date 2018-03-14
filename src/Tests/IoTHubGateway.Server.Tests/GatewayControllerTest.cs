using IoTHubGateway.Server.Controllers;
using IoTHubGateway.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace IoTHubGateway.Server.Tests
{
    public class GatewayControllerTest
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        GatewayController SetupWithSasToken(GatewayController controller, string sas_token)
        {
            var controllerContext = new ControllerContext();            
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add(Constants.SasTokenHeaderName, sas_token);
            controller.ControllerContext.HttpContext = httpContext;

            return controller;
        }

        [Fact]
        public async Task Send_WithoutDeviceToken_ReturnsBadRequest_If_SharedAccessPolicy_Is_Not_Enabled()
        {
            var gatewayService = new Mock<IGatewayService>();
            var options = new ServerOptions()
            {
                SharedAccessPolicyKeyEnabled = false,
            };

            var target = new GatewayController(gatewayService.Object, Options.Create<ServerOptions>(options));
            target.ControllerContext.HttpContext = new DefaultHttpContext();

            var result = await target.Send("device-1", new { payload = 1 });
            Assert.IsType<BadRequestObjectResult>(result);
        }


        [Fact]
        public async Task Send_WithDeviceToken_ReturnsOk_If_SharedAccessPolicy_Is_Not_Enabled()
        {
            var gatewayService = new Mock<IGatewayService>();
            var options = new ServerOptions()
            {
                SharedAccessPolicyKeyEnabled = false,
            };

            var target = new GatewayController(gatewayService.Object, Options.Create<ServerOptions>(options));
            target.ControllerContext.HttpContext = new DefaultHttpContext();
            target.ControllerContext.HttpContext.Request.Headers[Constants.SasTokenHeaderName] = "a-token";

            var result = await target.Send("device-1", new { payload = 1 });
            Assert.IsType<OkResult>(result);
        }


        [Fact]
        public async Task Send_WithoutDeviceToken_SendsUsingSharedAccessPolicy()
        {
            var gatewayService = new Mock<IGatewayService>();
            gatewayService.Setup(x => x.SendDeviceToCloudMessageBySharedAccess(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var options = new ServerOptions()
            {
                SharedAccessPolicyKeyEnabled = true,
            };

            var target = new GatewayController(gatewayService.Object, Options.Create<ServerOptions>(options));
            target.ControllerContext.HttpContext = new DefaultHttpContext();

            var result = await target.Send("device-1", new { payload = 1 });
            Assert.IsType<OkResult>(result);
            gatewayService.Verify();
        }

        [Fact]
        public async Task Send_WithDeviceToken_SendsUsingSharedAccessPolicy()
        {
            var gatewayService = new Mock<IGatewayService>();
            gatewayService.Setup(x => x.SendDeviceToCloudMessageByToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var options = new ServerOptions()
            {
                SharedAccessPolicyKeyEnabled = true,
            };

            var target = new GatewayController(gatewayService.Object, Options.Create<ServerOptions>(options));
            target.ControllerContext.HttpContext = new DefaultHttpContext();
            target.ControllerContext.HttpContext.Request.Headers.Add(Constants.SasTokenHeaderName, "a-token");

            var result = await target.Send("device-1", new { payload = 1 });
            Assert.IsType<OkResult>(result);
            gatewayService.Verify();
        }


        [Fact]
        public async Task Send_WithDeviceToken_And_TokenExpirationDateInPast_Returns_BadRequest()
        {
            var gatewayService = new Mock<IGatewayService>();
            var options = new ServerOptions()
            {
                SharedAccessPolicyKeyEnabled = false,
            };

            var target = new GatewayController(gatewayService.Object, Options.Create<ServerOptions>(options));
            target.ControllerContext.HttpContext = new DefaultHttpContext();
            target.ControllerContext.HttpContext.Request.Headers.Add(Constants.SasTokenHeaderName, "a-token");
            target.ControllerContext.HttpContext.Request.Headers.Add(Constants.SasTokenExpirationHeaderName, ((long)DateTime.UtcNow.AddDays(-1).Subtract(epoch).TotalSeconds).ToString());

            var result = await target.Send("device-1", new { payload = 1 });
            Assert.IsType<BadRequestObjectResult>(result);
        }


        [Fact]
        public async Task Send_WithDeviceToken_And_ValidTokenExpirationDate_Returns_OK()
        {
            var gatewayService = new Mock<IGatewayService>();

            var tokenExpirationDate = DateTime.UtcNow.AddMinutes(10);

            gatewayService.Setup(x => x.SendDeviceToCloudMessageByToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.Is<DateTime>(v => IsInSameSecond(v, tokenExpirationDate))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var options = new ServerOptions()
            {
                SharedAccessPolicyKeyEnabled = false,
            };

            var target = new GatewayController(gatewayService.Object, Options.Create<ServerOptions>(options));
            target.ControllerContext.HttpContext = new DefaultHttpContext();
            target.ControllerContext.HttpContext.Request.Headers.Add(Constants.SasTokenHeaderName, "a-token");
            target.ControllerContext.HttpContext.Request.Headers.Add(Constants.SasTokenExpirationHeaderName, ((long)tokenExpirationDate.Subtract(epoch).TotalSeconds).ToString());

            var result = await target.Send("device-1", new { payload = 1 });
            Assert.IsType<OkResult>(result);
            gatewayService.Verify();
        }

        private bool IsInSameSecond(DateTime date1, DateTime date2)
        {
            return date1.Subtract(date2).TotalSeconds < 1.0;
        }
    }
}
