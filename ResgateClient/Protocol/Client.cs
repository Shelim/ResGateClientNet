using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Resgate.Base;

namespace Resgate.Protocol
{
    public sealed class Client : IDisposable
    {
        public readonly Settings Settings;

        public Client(Settings settings)
        {
            Settings = settings;
            var baseSettings = new Base.Settings(settings.UriProvider, settings.ReconnectTimeout);
            baseSettings.Disconnected += BaseClientDisconnected;
            baseSettings.Reconnected += BaseClientReconnected;
            baseSettings.MessageReceived += BaseClientMessageReceived;
            baseClient = new Base.Client(baseSettings);
        }

        private void BaseClientDisconnected(object sender, DisconnectedEventArgs e)
        {
            Settings.InvokeConnectionLost(this);
            lock (unhandledPackets)
            {
                foreach (var unhandled in unhandledPackets)
                {
                    unhandled.Value.Failed(e.Info.Exception ?? new Exception("Client failed"));
                }

                unhandledPackets.Clear();
            }
        }

        private void BaseClientReconnected(object sender, ReconnectedEventArgs e)
        {
            bool success = false;
            Task.Run(async () =>
            {
                try
                {
                    lastMessage = 1;
                    var response = JsonConvert.DeserializeObject<ResponseVersion>(await SendCommand("version", null, null,
                        new ParamVersion {Protocol = Settings.DesiredVersion}));
                    if (response.Result == null || response.Result.Protocol != Settings.DesiredVersion)
                    {
                        Settings.InvokeFailed(this, FailedEventArgs.FailedReason.UnsupportedVersion);
                    }
                    else
                    {
                        Task.Run(() =>
                        {
                            Settings.InvokeConnectionReestablished(this);
                        });
                    }
                }
                catch (Exception ex)
                {
                    Settings.InvokeFailed(this, FailedEventArgs.FailedReason.VersionNegotiationFailed);
                }
            });
        }

        private void BaseClientMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var response = JsonConvert.DeserializeObject<ResponseBase>(e.Msg.Text);

            if (response.Id == 0)
            {
                Settings.InvokeEventReceived(this, e.Msg.Text);
            }
            else
            {
                Packet packet = null;
                lock (unhandledPackets)
                {
                    if (unhandledPackets.TryGetValue(response.Id, out packet))
                    {
                        unhandledPackets.Remove(response.Id);
                    }
                }
                if(packet != null)
                    packet.ResponseReceived(e.Msg.Text);
            }
        }
        public async Task<string> SendCommand(string type, string resourceId = null, string method = null, object param = null)
        {
            var packet = new Packet(lastMessage++, type, resourceId, method, param);
            lock (unhandledPackets)
            {
                unhandledPackets.Add(packet.Id, packet);
            }

            await baseClient.Send(packet.Content);

            if (!await packet.Handled.WaitAsync(Settings.ResponseTimeout))
            {
                throw new TimeoutException("Timeout for reply exceeded");
            }

            if (packet.Exception != null)
                throw packet.Exception;

            return packet.Response;
        }
        public sealed class ParamVersion
        {
            [JsonProperty("protocol")]
            public string Protocol;
        }
        private class ResponseBase
        {
            [JsonProperty("id")]
            public int Id;
        }
        private sealed class ResponseVersion : ResponseBase
        {
            public sealed class VersionResult
            {
                [JsonProperty("protocol")]
                public string Protocol;
            }

            [JsonProperty("result")]
            public VersionResult Result;
        }

        private readonly Base.Client baseClient;
        private int lastMessage = 1;
        private readonly Dictionary<int, Packet> unhandledPackets = new Dictionary<int, Packet>();

        public void Dispose()
        {
            baseClient.Dispose();
        }
    }
}
