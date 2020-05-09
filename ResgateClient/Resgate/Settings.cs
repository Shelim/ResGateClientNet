using System;
using System.Collections.Generic;
using System.Text;
using Resgate.Protocol;

namespace Resgate
{
    public sealed class Settings
    {
        public readonly Func<Uri> UriProvider;
        public readonly TimeSpan ReconnectTimeout;
        public readonly TimeSpan ResponseTimeout;

        public event EventHandler<FailedEventArgs> Failed;

        internal void InvokeFailed(Client client, FailedEventArgs.FailedReason reason)
        {
            Failed?.Invoke(client, new FailedEventArgs(reason));
        }

        public Settings(Func<Uri> uriProvider)
        {
            UriProvider = uriProvider;
            ReconnectTimeout = TimeSpan.FromSeconds(1);
            ResponseTimeout = TimeSpan.FromSeconds(5);
        }

        public Settings(Func<Uri> uriProvider, TimeSpan reconnectTimeout, TimeSpan responseTimeout)
        {
            UriProvider = uriProvider;
            ReconnectTimeout = reconnectTimeout;
            ResponseTimeout = responseTimeout;
        }
    }
}