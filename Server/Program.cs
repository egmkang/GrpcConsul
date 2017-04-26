using System;
using System.Threading.Tasks;
using Consul;
using Grpc.Core;
using Helloworld;

namespace Server
{
    internal class GreeterImpl : Greeter.GreeterBase
    {
        // Server side handler of the SayHello RPC
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
        }
    }

    internal class Program
    {
        public static void Main(string[] args)
        {
            var port = int.Parse(args[0]);

            var yellowPages = new YellowPages();
            var server = new Grpc.Core.Server
                             {
                                 Services = { Greeter.BindService(new GreeterImpl()) },
                                 Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
                             };

            yellowPages.RegisterService("helloworld.Greeter", port);

            server.Start();

            Console.WriteLine("Greeter server listening on port " + port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}