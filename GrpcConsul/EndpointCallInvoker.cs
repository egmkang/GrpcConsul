using System;
using Grpc.Core;

namespace GrpcConsul
{
    internal sealed class EndpointCallInvoker : CallInvoker
    {
        private readonly IEndpointStrategy _endpointStrategy;

        public EndpointCallInvoker(IEndpointStrategy endpointStrategy)
        {
            _endpointStrategy = endpointStrategy;
        }

        private TResponse Call<TResponse>(string serviceName, Func<CallInvoker, TResponse> call)
        {
            var callInvoker = _endpointStrategy.Get(serviceName);
            try
            {
                return call(callInvoker);
            }
            catch (RpcException ex)
            {
                // forget channel if unavailable
                if (ex.Status.StatusCode == StatusCode.Unavailable)
                {
                    _endpointStrategy.Revoke(serviceName);
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