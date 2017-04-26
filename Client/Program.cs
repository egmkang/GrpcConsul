using System;
using Consul;
using Grpc.Core;
using Helloworld;

namespace Client
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var yellowPages = new YellowPages();
            for (int i = 0; i<10; ++i)
            {
                var endpoint = yellowPages.FindServiceEndpoint("helloworld.Greeter");
                Console.WriteLine(endpoint);
            }


            var consulChannels = new ConsulChannels(yellowPages);
            var consulCallInvoker = new ConsulCallInvoker(consulChannels);
            var client = new Greeter.GreeterClient(consulCallInvoker);
            var user = "you";

            var reply = client.SayHello(new HelloRequest { Name = user });
            Console.WriteLine("Greeting: " + reply.Message);

            consulChannels.Shutdown();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}