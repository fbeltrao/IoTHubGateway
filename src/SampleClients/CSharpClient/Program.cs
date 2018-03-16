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
            var hostName = "<enter-iothub-name>";
            var deviceId = "<enter-device-id>";
            var tokenttl = DateTime.UtcNow.AddSeconds(10);
            var sasToken = new SharedAccessSignatureBuilder()
            {
                Key = "",
                Target = $"{hostName}.azure-devices.net/devices/{deviceId}",
                TimeToLive = TimeSpan.FromMinutes(20)
            }
            .ToSignature();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("sas_token", sasToken);
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
