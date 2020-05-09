using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Resgate.Protocol;
using Resgate.Utility;

namespace Resgate
{
    public sealed class Client : IDisposable
    {
        public readonly Settings Settings;
        public Client(Settings settings)
        {
            Settings = settings;

            var protocolSettings = new Protocol.Settings(settings.UriProvider, settings.ReconnectTimeout,
                settings.ResponseTimeout);

            protocolSettings.ConnectionLost += ClientConnectionLost;
            protocolSettings.ConnectionReestablished += ClientConnectionReestablished;
            protocolSettings.EventReceived += ClientEventReceived;
            protocolSettings.Failed += ClientFailed;

            isConnected = false;

            state.StateInitial += StateOnStateInitial;
            state.StateChanged += StateOnStateChanged;
            state.StateColInitial += StateOnStateColInitial;
            state.StateColAdded += StateOnStateColAdded;
            state.StateColChanged += StateOnStateColChanged;
            state.StateColRemoved += StateOnStateColRemoved;

            protocolClient = new Protocol.Client(protocolSettings);
        }

        private void StateOnStateColRemoved(object sender, StateColRemovedEventArgs e)
        {
            if (subscriptionCollection.TryGetReverse(e.Rid, out var collections))
            {
                foreach (var collection in collections)
                {
                    collection.Removed?.Invoke(e.Index);
                }
            }
        }

        private void StateOnStateColChanged(object sender, StateColChangedEventArgs e)
        {
            if (subscriptionCollection.TryGetReverse(e.Rid, out var collections))
            {
                foreach (var collection in collections)
                {
                    collection.Changed?.Invoke(e.Index, e.Obj);
                }
            }
        }

        private void StateOnStateColAdded(object sender, StateColAddedEventArgs e)
        {
            if (subscriptionCollection.TryGetReverse(e.Rid, out var collections))
            {
                foreach (var collection in collections)
                {
                    collection.Added?.Invoke(e.Index, e.Obj);
                }
            }
        }

        private void StateOnStateColInitial(object sender, StateColInitialEventArgs e)
        {
            if (subscriptionCollection.TryGetReverse(e.Rid, out var collections))
            {
                foreach (var collection in collections)
                {
                    collection.Initial?.Invoke(e.Obj);
                }
            }
        }

        private void StateOnStateChanged(object sender, StateChangedEventArgs e)
        {
            if(subscriptionModel.TryGetReverse(e.Rid, out var models))
            {
                foreach (var model in models)
                {
                    model.Changed?.Invoke(e.Obj);
                }
            }
        }

        private void StateOnStateInitial(object sender, StateInitialEventArgs e)
        {
            if (subscriptionModel.TryGetReverse(e.Rid, out var models))
            {
                foreach (var model in models)
                {
                    model.Initial?.Invoke(e.Obj);
                }
            }
        }

        private bool isConnected;

        private void ClientFailed(object sender, FailedEventArgs e)
        {
            Settings.InvokeFailed(this, e.Reason);
        }

        private void ClientEventReceived(object sender, EventReceivedEventArgs e)
        {
            state.EventCalled(e.Msg);
        }

        private void ClientConnectionReestablished(object sender, ConnectionReestablishedEventArgs e)
        {
            lock (subscriptionModel)
            {
                lock (subscriptionCollection)
                {
                    isConnected = true;

                    foreach (var model in subscriptionModel.Reverse())
                    {
                        var data = protocolClient.SendCommand("subscribe", model.Key).Result;
                        state.UpdateData(data);
                    }

                    foreach (var collection in subscriptionCollection.Reverse())
                    {
                        var data = protocolClient.SendCommand("subscribe", collection.Key).Result;
                        state.UpdateData(data);
                    }
                }
            }
        }

        private void ClientConnectionLost(object sender, ConnectionLostEventArgs e)
        {
            lock (subscriptionModel)
            {
                lock (subscriptionCollection)
                {
                    isConnected = false;
                }
            }
        }

        public void Dispose()
        {
            protocolClient.Dispose();
        }

        public async Task<TokenModel> SubscribeModel<T>(string rid, Action<T> initial, Action<T> changed)
        {
            var token = new TokenModel(this, rid, data =>
            {
                initial?.Invoke(data.ToObject<T>());
            }, data =>
            {
                changed?.Invoke(data.ToObject<T>());
            });

            bool connected;

            lock (subscriptionModel)
            {
                subscriptionModel.Add(token, rid);
                connected = isConnected;
            }

            if (connected)
            {
                var data = await protocolClient.SendCommand("subscribe", rid);
                state.UpdateData(data);
            }

            return token;
        }

        public async Task<TokenCollection> SubscribeCollection<T>(string rid, Action<List<T>> initial,
            Action<int, T> added, Action<int, T> changed, Action<int> removed)
        {
            var token = new TokenCollection(this, rid, (list) =>
            {
                initial?.Invoke(list.Select(x => x.ToObject<T>()).ToList());
            }, (idx, data) =>
            {
                added?.Invoke(idx, data.ToObject<T>());
            }, (idx, data) =>
            {
                changed?.Invoke(idx, data.ToObject<T>());
            }, (idx) => { removed?.Invoke(idx); });

            bool connected;

            lock (subscriptionCollection)
            {
                subscriptionCollection.Add(token, rid);
                connected = isConnected;
            }

            if (connected)
            {
                var data = await protocolClient.SendCommand("subscribe", rid);
                state.UpdateData(data);
            }

            return token;
        }

        internal void UnsubscribeToken(TokenModel tokenModel)
        {
            bool connected;

            lock (subscriptionModel)
            {
                subscriptionModel.RemoveForward(tokenModel);
                connected = isConnected;
            }

            if (connected)
            {
                Task.Run(async () =>
                {
                    await protocolClient.SendCommand("unsubscribe", tokenModel.Rid);
                });
            }
        }

        internal void UnsubscribeToken(TokenCollection tokenCollection)
        {
            bool connected;

            lock (subscriptionCollection)
            {
                subscriptionCollection.RemoveForward(tokenCollection);
                connected = isConnected;
            }

            if (connected)
            {
                Task.Run(async () => { 
                    await protocolClient.SendCommand("unsubscribe", tokenCollection.Rid);
                });
            }
        }

        private readonly Protocol.Client protocolClient;
        private readonly State state = new State();
        private readonly BijectiveDictionary<TokenModel, string> subscriptionModel = new BijectiveDictionary<TokenModel, string>();
        private readonly BijectiveDictionary<TokenCollection, string> subscriptionCollection = new BijectiveDictionary<TokenCollection, string>();
    }
}
