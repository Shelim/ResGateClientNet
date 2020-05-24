using System;
using System.Collections.Generic;
using System.Text;
using Resgate.Protocol;
using Resgate.Resgate;

namespace Resgate
{
    public sealed class Settings
    {
        public readonly Func<Uri> UriProvider;
        public readonly TimeSpan ReconnectTimeout;
        public readonly TimeSpan ResponseTimeout;

        public event EventHandler<FailedEventArgs> Failed;
        public event EventHandler<ErrorEventArgs> Error;

        internal void InvokeFailed(Client client, FailedEventArgs.FailedReason reason)
        {
            Failed?.Invoke(client, new FailedEventArgs(reason));
        }
        internal void InvokeError(Client client, string rid, ErrorException error)
        {
            Error?.Invoke(client, new ErrorEventArgs(rid, error));
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