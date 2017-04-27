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
            var consulChannels = new ConsulCallInvoker(serviceDiscovery);
            var consulCallInvoker = new StickyCallInvoker(consulChannels);
            var client = new Greeter.GreeterClient(consulCallInvoker);

            var attempt = 0;
            while (true)
            {
                ++attempt;
                Console.WriteLine($"=== Attempt {attempt} (Press any key) ===");
                Console.ReadKey();

                try
                {
                    var reply = client.SayHello(new HelloRequest { Name = $"Attempt {attempt}" });
                    Console.WriteLine($"Success: {reply.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failure: {ex.Message}");
                }
            }
        }
    }
}