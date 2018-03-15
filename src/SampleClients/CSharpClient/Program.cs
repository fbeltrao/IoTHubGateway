using Microsoft.Azure.Devices.Client;
using System;
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
            var hostName = "iotedgetest";
            var deviceId = "amqp1005";
            var tokenttl = DateTime.UtcNow.AddSeconds(10);
            var sasToken = new SharedAccessSignatureBuilder()
            {
                Key = "rIN0MnWXSB8VwEGcjFPodSdgqf9AQGJGzTRD8K1pDtQ=",
                Target = $"{hostName}.azure-devices.net/devices/{deviceId}",
                //TimeToLive = TimeSpan.FromMinutes(20)
                TimeToLive = TimeSpan.FromSeconds(10)
            }
            .ToSignature();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("sas_token", sasToken);
                //client.DefaultRequestHeaders.Add("sas_token_expiration", sasToken);
                while (true)
                {
                    var postResponse = await client.PostAsync($"http://localhost:32527/api/{deviceId}", new StringContent("{ content: 'from_rest_call' }", Encoding.UTF8, "application/json"));
                    Console.WriteLine($"Response: {postResponse.StatusCode.ToString()}");

                    await Task.Delay(200);

                }
            }
        }
    }
}
