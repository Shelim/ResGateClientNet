using System;
using System.Collections.Generic;
using System.Text;
using Websocket.Client;
using Websocket.Client.Models;


namespace Resgate.Base
{
    public sealed class Settings
    {
        public readonly Func<Uri> UriProvider;
        public readonly TimeSpan ReconnectTimeout;

        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<ReconnectedEventArgs> Reconnected;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<MessageSentEventArgs> MessageSent;

        internal void InvokeDisconnected(Client client, DisconnectionInfo info)
        {
            Disconnected?.Invoke(client, new DisconnectedEventArgs(info));
        }

        internal void InvokeReconnected(Client client, ReconnectionInfo info)
        {
            Reconnected?.Invoke(client, new ReconnectedEventArgs(info));
        }

        internal void InvokeMessageReceived(Client client, ResponseMessage msg)
        {
            MessageReceived?.Invoke(client, new MessageReceivedEventArgs(msg));
        }

        internal void InvokeMessageSent(Client client, string msg)
        {
            MessageSent?.Invoke(client, new MessageSentEventArgs(msg));
        }

        internal Uri GetNextUri()
        {
            return UriProvider?.Invoke();
        }

        public Settings(Func<Uri> uriProvider)
        {
            UriProvider = uriProvider;
            ReconnectTimeout = TimeSpan.FromSeconds(1);
        }

        public Settings(Func<Uri> uriProvider, TimeSpan reconnectTimeout)
        {
            UriProvider = uriProvider;
            ReconnectTimeout = reconnectTimeout;
        }
    }
}