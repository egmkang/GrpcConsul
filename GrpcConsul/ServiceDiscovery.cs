using System;
using System.Linq;
using System.Net;
using Consul;

namespace GrpcConsul
{
    public sealed class ServiceDiscovery
    {
        private readonly ConsulClient _client;
        private readonly Random _rnd = new Random();

        public ServiceDiscovery()
        {
            _client = new ConsulClient();
        }

        public string GetHostName()
        {
            return Dns.GetHostName();
        }

        public Entry RegisterService(string name, int port)
        {
            var hostName = Dns.GetHostName();
            var serviceId = $"{hostName}-{name}-{port}";
            var asr = new AgentServiceRegistration
                          {
                              Address = hostName,
                              ID = serviceId,
                              Name = name,
                              Port = port
                          };

            var res = _client.Agent.ServiceRegister(asr).Result;
            if (res.StatusCode != HttpStatusCode.OK)
            {
                throw new ApplicationException($"Failed to register service {name} on port {port}");
            }

            return new Entry(this, name, port, serviceId);
        }

        public void UnregisterService(string serviceId)
        {
            var res = _client.Agent.ServiceDeregister(serviceId).Result;
        }

        public string FindServiceEndpoint(string name)
        {
            var res = _client.Agent.Services().Result;
            if (res.StatusCode != HttpStatusCode.OK)
            {
                throw new ApplicationException($"Failed to query services");
            }

            var services = res.Response.Values.Where(x => x.Service == name).ToArray();
            if (0 == services.Length)
            {
                throw new ApplicationException($"Can't find service {name}");
            }

            var rnd = _rnd.Next(services.Length);
            var service = services[rnd];
            return $"{service.Address}:{service.Port}";
        }

        public sealed class Entry : IDisposable
        {
            private readonly ServiceDiscovery _serviceDiscovery;

            internal Entry(ServiceDiscovery serviceDiscovery, string serviceName, int port, string serviceId)
            {
                ServiceName = serviceName;
                Port = port;
                ServiceId = serviceId;
                _serviceDiscovery = serviceDiscovery;
            }

            public string ServiceName { get; }
            public int Port { get; }
            public string ServiceId { get; }

            public void Dispose()
            {
                _serviceDiscovery.UnregisterService(ServiceId);
            }
        }
    }
}