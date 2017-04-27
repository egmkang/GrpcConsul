using System;

namespace GrpcConsul
{
    public class ClientFactory
    {
        private readonly EndpointCallInvoker _callInvoker;

        public ClientFactory(IEndpointStrategy strategy)
        {
            _callInvoker = new EndpointCallInvoker(strategy);
        }

        public T Get<T>()
        {
            var client = (T) Activator.CreateInstance(typeof(T), _callInvoker);
            return client;
        }
    }
}