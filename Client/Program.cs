using System;
using GrpcConsul;
using Helloworld;

namespace Client
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var serviceDiscovery = new ServiceDiscovery();
            var endpointStrategy = new StickyEndpointStrategy(serviceDiscovery);
            var clientFactory = new ClientFactory(endpointStrategy);
            var client = clientFactory.Get<Greeter.GreeterClient>();

            var attempt = 0;
            while (true)
            {
                ++attempt;
                Console.WriteLine($"=== Attempt {attempt} ===");

                try
                {
                    var reply = client.SayHello(new HelloRequest { Name = $"Attempt {attempt}" });
                    Console.WriteLine($"Success: {reply.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failure: {ex.Message}");
                }

                System.Threading.Thread.Sleep(1 * 1000);
            }
        }
    }
}