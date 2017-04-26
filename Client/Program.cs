using System;
using Grpc.Core;
using GrpcConsul;
using Helloworld;

namespace Client
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var yellowPages = new YellowPages();
            for (var i = 0; i < 10; ++i)
            {
                var endpoint = yellowPages.FindServiceEndpoint("helloworld.Greeter");
                Console.WriteLine($"Endpoint: {endpoint}");
            }

            var consulChannels = new ConsulChannels(yellowPages);
            var consulCallInvoker = new ConsulCallInvoker(consulChannels);
            var client = new Greeter.GreeterClient(consulCallInvoker);

            var attempt = 0;
            while (true)
            {
                ++attempt;

                try
                {
                    var reply = client.SayHello(new HelloRequest { Name = $"Attempt {attempt}" });
                    Console.WriteLine("Greeting: " + reply.Message);
                    Console.ReadLine();
                }
                catch (RpcException ex)
                {
                    Console.WriteLine($"Failed with error {ex}");
                }
            }
        }
    }
}