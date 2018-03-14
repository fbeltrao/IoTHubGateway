using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CSharpClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("<PRESS ENTER TO CONTINUE>");
            Console.ReadLine();
            var hostName = "add-your-iothub-name-here";
            var deviceId = "add-your-device-id-here";
            var sasToken = new SharedAccessSignatureBuilder()
            {
                Key = "add-your-device-key-here",
                Target = $"{hostName}.azure-devices.net/devices/{deviceId}",
                TimeToLive = TimeSpan.FromMinutes(5)
            }
            .ToSignature();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("sas_token", sasToken);
                var postResponse = await client.PostAsync($"http://localhost:32527/api/{deviceId}", new StringContent("{ content: 'from_rest_call' }", Encoding.UTF8, "application/json"));
            }
        }
    }
}
