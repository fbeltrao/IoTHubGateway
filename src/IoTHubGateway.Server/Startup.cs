﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoTHubGateway.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IoTHubGateway.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddMemoryCache();

            // A single instance of registered devices must be kept
            services.AddSingleton<RegisteredDevices>();

            // Resolve server options
            var options = new ServerOptions();
            Configuration.GetSection(nameof(ServerOptions)).Bind(options);
            services.AddSingleton<ServerOptions>(options);


            if (options.CloudMessagesEnabled)
            {
                services.AddSingleton<IHostedService, CloudToMessageListenerJobHostedService>();
            }

#if DEBUG
            SetupDebugListeners(options);
            
#endif

            services.AddSingleton<IGatewayService, GatewayService>();
            services.AddMvc();
        }

#if DEBUG
        private void SetupDebugListeners(ServerOptions options)
        {
            if (options.DirectMethodEnabled && options.DirectMethodCallback == null)
            {
                options.DirectMethodCallback = (methodRequest, userContext) =>
                {
                    var deviceId = (string)userContext;
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Device method for {deviceId}.{methodRequest.Name}({methodRequest.DataAsJson}) received");

                    var responseBody = "{ succeeded: true }";
                    MethodResponse methodResponse = new MethodResponse(Encoding.UTF8.GetBytes(responseBody), 200);

                    return Task.FromResult(methodResponse);
                };
            }

            if (options.CloudMessagesEnabled && options.CloudMessageCallback == null)
            {
                options.CloudMessageCallback = (deviceId, message) =>
                {
                    Console.WriteLine($"[{DateTime.Now.ToString()}] Cloud message for {deviceId} received");
                };
            }
        }
#endif

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }            
            

            app.UseMvc();
        }
    }
}
