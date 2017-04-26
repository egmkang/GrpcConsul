using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Grpc.Core;

namespace GrpcConsul
{
    public class ConsulChannels
    {
        private readonly object _lock = new object();
        private readonly YellowPages _yellowPages;
        private readonly Dictionary<string, DefaultCallInvoker> _invokers = new Dictionary<string, DefaultCallInvoker>();
        private readonly Dictionary<string, Channel> _channels = new Dictionary<string, Channel>();

        public ConsulChannels(YellowPages yellowPages)
        {
            _yellowPages = yellowPages;
        }

        public CallInvoker GetOrCreate(string serviceName)
        {
            lock (_lock)
            {
                // find callInvoker first if any
                if (_invokers.TryGetValue(serviceName, out DefaultCallInvoker callInvoker))
                {
                    return callInvoker;
                }

                // find a (shared) channel for target if any
                var target = _yellowPages.FindServiceEndpoint(serviceName);
                if (! _channels.TryGetValue(target, out Channel channel))
                {
                    channel = new Channel(target, ChannelCredentials.Insecure);
                    _channels.Add(target, channel);
                }

                // build a new call invoker + channel
                callInvoker = new DefaultCallInvoker(channel);
                _invokers.Add(serviceName, callInvoker);
                return callInvoker;
            }
        }

        public void Release(string serviceName)
        {
            lock (_lock)
            {
                // find callInvoker first if any
                if (!_invokers.TryGetValue(serviceName, out DefaultCallInvoker callInvoker))
                {
                    return;
                }

                // a bit hackish
                var channelFieldInfo = callInvoker.GetType().GetField("channel", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                var channel = (Channel)channelFieldInfo.GetValue(callInvoker);

                // get rid of channel & invoker
                _channels.Remove(channel.Target);
                _invokers.Remove(serviceName);
                channel.ShutdownAsync();

                // TODO: blacklist target for a while
            }
        }
    }
}