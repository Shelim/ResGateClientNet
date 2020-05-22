using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Websocket.Client;
using Websocket.Client.Models;

namespace Resgate.Base
{
    public sealed class Client : IDisposable
    {
        public readonly Settings Settings;

        public Client(Settings settings)
        {
            Settings = settings;

            client = new WebsocketClient(Settings.GetNextUri());

            client.ReconnectTimeout = null;
            client.ErrorReconnectTimeout = TimeSpan.FromSeconds(1);
            client.DisconnectionHappened.Subscribe(info =>
            {
                client.Url = Settings.GetNextUri();
                Settings.InvokeDisconnected(this, info);
            });
            client.ReconnectionHappened.Subscribe(info => { Settings.InvokeReconnected(this, info); });

            client.MessageReceived.Subscribe(msg => { Settings.InvokeMessageReceived(this, msg); });
            client.Start();
        }

        public async Task Reconnect()
        {
            await client.Reconnect();
        }

        public async Task Send(string messageJson)
        {
            Settings.InvokeMessageSent(this, messageJson);
            client.Send(messageJson);

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            client.Dispose();
        }

        private WebsocketClient client;
    }
}