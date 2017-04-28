using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Grpc.Core;

namespace GrpcConsul
{
    public class StickyEndpointStrategy : IEndpointStrategy
    {
        private readonly ServiceDiscovery _serviceDiscovery;
        private readonly ConcurrentDictionary<string, DefaultCallInvoker> _invokers = new ConcurrentDictionary<string, DefaultCallInvoker>();

        private readonly object _lock = new object();
        private readonly Dictionary<string, Channel> _channels = new Dictionary<string, Channel>();

        public StickyEndpointStrategy(ServiceDiscovery serviceDiscovery)
        {
            _serviceDiscovery = serviceDiscovery;
        }

        public CallInvoker Get(string serviceName)
        {
            // find callInvoker first if any (fast path)
            if (_invokers.TryGetValue(serviceName, out var callInvoker))
            {
                return callInvoker;
            }

            // no luck (slow path): either no call invoker available or a shutdown is in progress
            lock (_lock)
            {
                // since can be considered a double-check lock
                if (_invokers.TryGetValue(serviceName, out callInvoker))
                {
                    return callInvoker;
                }

                // find a (shared) channel for target if any
                var target = _serviceDiscovery.FindServiceEndpoint(serviceName);

                if (!_channels.TryGetValue(target, out var channel))
                {
                    channel = new Channel(target, ChannelCredentials.Insecure);
                    _channels.Add(target, channel);
                }

                // build a new call invoker + channel
                callInvoker = new DefaultCallInvoker(channel);
                _invokers.TryAdd(serviceName, callInvoker);

                return callInvoker;
            }
        }

        public void Revoke(string serviceName, CallInvoker failedCallInvoker)
        {
            lock (_lock)
            {
                // only destroy the call invoker if & only if it is still published
                if (!_invokers.TryGetValue(serviceName, out var callInvoker) || !ReferenceEquals(callInvoker, failedCallInvoker))
                {
                    return;
                }

                // a bit hackish
                var channelFieldInfo = failedCallInvoker.GetType().GetField("channel", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                var failedChannel = (Channel) channelFieldInfo.GetValue(failedCallInvoker);

                // shutdown the channel - this must be done once
                if(_channels.TryGetValue(failedChannel.Target, out var channel) && ReferenceEquals(channel, failedChannel))
                {
                    _channels.Remove(channel.Target);
                    channel.ShutdownAsync();
                }

                _invokers.TryRemove(serviceName, out callInvoker);
                _serviceDiscovery.Blacklist(channel.Target);
            }
        }
    }
}