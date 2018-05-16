using Microsoft.Azure.Devices.Client;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("<PRESS ENTER TO CONTINUE>");
            Console.ReadLine();
            
            //await SendUsingDeviceToken();
            SendUsingConnectionString();
        }


        private static void SendUsingConnectionString()
        {
            var connectionString = "HostName=xxxxx.azure-devices.net;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
            //var deviceId = "device0001";

            using (var client = new HttpClient())
            {
                var range = Enumerable.Range(1, 200);
                Parallel.ForEach(range, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, (index, loopState) =>
                {
                    try
                    {
                        var deviceId = "device" + index.ToString("0000000");                       
                        var content = new StringContent("{ content: 'from_rest_call' }", Encoding.UTF8, "application/json");
                        content.Headers.Add("connection_string", connectionString);
                        var postResponse = client.PostAsync($"http://localhost:5000/api/{deviceId}", content).GetAwaiter().GetResult();
                        Console.WriteLine($"Device: {deviceId}, Response: {postResponse.StatusCode.ToString()}");

                        //await Task.Delay(100);
                        Thread.Sleep(100);                                            
                    }
                    catch { }
                });

            }
             


           
        }

        private static async Task SendUsingDeviceToken()
        {
            var hostName = "<enter-iothub-name>";
            var deviceId = "<enter-device-id>";

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
                    var postResponse = await client.PostAsync($"http://localhost:5000/api/{deviceId}", new StringContent("{ content: 'from_rest_call' }", Encoding.UTF8, "application/json"));
                    Console.WriteLine($"Response: {postResponse.StatusCode.ToString()}");

                    await Task.Delay(5 * 1000);

                }
            }
        }
    }
}
