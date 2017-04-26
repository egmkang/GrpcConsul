using System.Collections.Generic;
using Grpc.Core;

namespace Consul
{
    public class ConsulChannels
    {
        private readonly YellowPages _yellowPages;
        private readonly object _lock = new object();
        private readonly Dictionary<string, CallInvoker> _channels = new Dictionary<string, CallInvoker>();

        public ConsulChannels(YellowPages yellowPages)
        {
            _yellowPages = yellowPages;
        }

        public CallInvoker Acquire(string serviceName)
        {
            lock (_lock)
            {
                CallInvoker callInvoker;
                if (!_channels.TryGetValue(serviceName, out callInvoker))
                {
                    var target = _yellowPages.FindServiceEndpoint(serviceName);
                    var channel = new Channel(target, ChannelCredentials.Insecure);
                    callInvoker = new DefaultCallInvoker(channel);
                    _channels.Add(serviceName, callInvoker);
                }

                return callInvoker;
            }
        }

        public void Release(string serviceName)
        {
            lock (_lock)
            {
                _channels.Remove(serviceName);
            }
        }

        public void Shutdown()
        {
        }
    }

    public class ConsulCallInvoker : CallInvoker
    {
        private readonly ConsulChannels _channels;

        public ConsulCallInvoker(ConsulChannels channels)
        {
            _channels = channels;
        }

        private CallInvoker GetCallInvoker<TRequest, TResponse>(Method<TRequest, TResponse> method)
        {
            return _channels.Acquire(method.ServiceName);
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            return GetCallInvoker(method).BlockingUnaryCall(method, host, options, request);
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            return GetCallInvoker(method).AsyncUnaryCall(method, host, options, request);
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            return GetCallInvoker(method).AsyncServerStreamingCall(method, host, options, request);
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            return GetCallInvoker(method).AsyncClientStreamingCall(method, host, options);
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            return GetCallInvoker(method).AsyncDuplexStreamingCall(method, host, options);
        }
    }
}