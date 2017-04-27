using Grpc.Core;

namespace GrpcConsul
{
    public interface IEndpointStrategy
    {
        CallInvoker Get(string serviceName);
        void Revoke(string serviceName);
    }
}