using System;
using Grpc.Core;

namespace GrpcConsul
{
    public sealed class ConsulCallInvoker : CallInvoker
    {
        private readonly ConsulChannels _channels;

        public ConsulCallInvoker(ConsulChannels channels)
        {
            _channels = channels;
        }

        private CallInvoker GetCallInvoker(string serviceName)
        {
            return _channels.GetOrCreate(serviceName);
        }

        private TResponse Call<TResponse>(string serviceName, Func<CallInvoker, TResponse> call)
        {
            var callInvoker = GetCallInvoker(serviceName);
            try
            {
                return call(callInvoker);
            }
            catch (RpcException ex)
            {
                // forget channel if unavailable
                if (ex.Status.StatusCode == StatusCode.Unavailable)
                {
                    _channels.Release(serviceName);
                }

                throw;
            }
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            return Call(method.ServiceName, ci => ci.BlockingUnaryCall(method, host, options, request));
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            return Call(method.ServiceName, ci => ci.AsyncUnaryCall(method, host, options, request));
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            return Call(method.ServiceName, ci => ci.AsyncServerStreamingCall(method, host, options, request));
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            return Call(method.ServiceName, ci => ci.AsyncClientStreamingCall(method, host, options));
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            return Call(method.ServiceName, ci => ci.AsyncDuplexStreamingCall(method, host, options));
        }
    }
}