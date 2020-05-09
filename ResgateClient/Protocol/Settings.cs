using System;
using System.Collections.Generic;
using System.Text;

namespace Resgate.Protocol
{
    public sealed class Settings
    {
        public readonly Func<Uri> UriProvider;
        public readonly TimeSpan ReconnectTimeout;
        public readonly TimeSpan ResponseTimeout;
        public readonly string DesiredVersion = "1.2.0";

        public event EventHandler<FailedEventArgs> Failed;
        public event EventHandler<EventReceivedEventArgs> EventReceived;
        public event EventHandler<ConnectionLostEventArgs> ConnectionLost;
        public event EventHandler<ConnectionReestablishedEventArgs> ConnectionReestablished;

        internal void InvokeFailed(Client client, FailedEventArgs.FailedReason reason)
        {
            Failed?.Invoke(client, new FailedEventArgs(reason));
        }
        internal void InvokeEventReceived(Client client, string msg)
        {
            EventReceived?.Invoke(client, new EventReceivedEventArgs(msg));
        }
        internal void InvokeConnectionLost(Client client)
        {
            ConnectionLost?.Invoke(client, new ConnectionLostEventArgs());
        }
        internal void InvokeConnectionReestablished(Client client)
        {
            ConnectionReestablished?.Invoke(client, new ConnectionReestablishedEventArgs());
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
