using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Resgate.Utility;

namespace Resgate
{
    internal abstract class StateEventArgs : EventArgs
    {
        public readonly string Rid;

        protected StateEventArgs(string rid)
        {
            Rid = rid;
        }
    }

    internal class StateInitialEventArgs : StateEventArgs
    {
        public readonly JToken Obj;

        public StateInitialEventArgs(string rid, JToken obj) : base(rid)
        {
            Obj = obj;
        }
    }
    internal class StateChangedEventArgs : StateEventArgs
    {
        public readonly JToken Obj;

        public StateChangedEventArgs(string rid, JToken obj) : base(rid)
        {
            Obj = obj;
        }
    }
    internal class StateColInitialEventArgs : StateEventArgs
    {
        public readonly List<JToken> Obj;

        public StateColInitialEventArgs(string rid, List<JToken> obj) : base(rid)
        {
            Obj = obj;
        }
    }
    internal class StateColAddedEventArgs : StateEventArgs
    {
        public readonly int Index;
        public readonly JToken Obj;

        public StateColAddedEventArgs(string rid, int index, JToken obj) : base(rid)
        {
            Index = index;
            Obj = obj;
        }
    }
    internal class StateColChangedEventArgs : StateEventArgs
    {
        public readonly int Index;
        public readonly JToken Obj;

        public StateColChangedEventArgs(string rid, int index, JToken obj) : base(rid)
        {
            Index = index;
            Obj = obj;
        }
    }
    internal class StateColRemovedEventArgs : StateEventArgs
    {
        public readonly int Index;

        public StateColRemovedEventArgs(string rid, int index) : base(rid)
        {
            Index = index;
        }
    }
    internal sealed class State
    {
        private readonly Dictionary<string, Dictionary<string, JToken>> models =
            new Dictionary<string, Dictionary<string, JToken>>();

        private readonly Dictionary<string, List<JToken>> collections = new Dictionary<string, List<JToken>>();

        public void UpdateDataFromSubscription(string data)
        {
            var parsed = JsonConvert.DeserializeObject<DataContainer>(data);

            AddNewSubscriptions(parsed.Result);
        }

        /*
        public void FireInitialStateForModel(string rid)
        {
            lock (initialModels)
            {
                if (initialModels.TryGetValue(rid, out var val))
                {
                    StateInitial?.Invoke(this, new StateInitialEventArgs(rid, val));
                    initialModels.Remove(rid);
                }
            }
        }
        public void FireInitialStateForCollection(string rid)
        {
            lock (initialCollections)
            {
                if (initialCollections.TryGetValue(rid, out var val))
                {
                    StateColInitial?.Invoke(this, new StateColInitialEventArgs(rid, val));
                    initialCollections.Remove(rid);
                }
            }
        }
        */
        public void UpdateDataFromGet(string data)
        {
            var parsed = JsonConvert.DeserializeObject<DataContainer>(data);

            AddNewData(parsed.Result);
        }


        public T GetModel<T>(string rid)
        {
            if (models.TryGetValue(rid, out var model))
            {
                return ResolveJToken(model).ToObject<T>();
            }

            return default(T);
        }
        public List<T> GetCollection<T>(string rid)
        {
            if (collections.TryGetValue(rid, out var collection))
            {
                return collection.Select(x => ResolveJToken(x).ToObject<T>()).ToList();
            }

            return new List<T>();
        }

        public event EventHandler<StateInitialEventArgs> StateInitial;
        public event EventHandler<StateChangedEventArgs> StateChanged;
        public event EventHandler<StateColInitialEventArgs> StateColInitial;
        public event EventHandler<StateColAddedEventArgs> StateColAdded;
        public event EventHandler<StateColChangedEventArgs> StateColChanged;
        public event EventHandler<StateColRemovedEventArgs> StateColRemoved;

        private readonly BijectiveDictionary<string, string> indirectSubscriptions = new BijectiveDictionary<string, string>();
        // private readonly Dictionary<string, JToken> initialModels = new Dictionary<string, JToken>();
        // private readonly Dictionary<string, List<JToken>> initialCollections = new Dictionary<string, List<JToken>>();
        private void AddIndirectSubscription(string parent, JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var rid = token.ToObject<ValueRid>();
                if (rid != null && !string.IsNullOrWhiteSpace(rid.Rid))
                {
                    indirectSubscriptions.Add(parent, rid.Rid);
                }
            }
        }

        private void RemoveIndirectSubscription(string parent, JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var rid = token.ToObject<ValueRid>();
                if (rid != null && !string.IsNullOrWhiteSpace(rid.Rid))
                {
                    indirectSubscriptions.Remove(parent, rid.Rid);
                }
            }
        }

        private JToken ResolveToken(JToken token, Dictionary<string, JToken> resolved)
        {
            if (token.Type == JTokenType.Object)
            {
                var rid = token.ToObject<ValueRid>();
                if (rid != null && !string.IsNullOrWhiteSpace(rid.Rid))
                {
                    if (resolved.TryGetValue(rid.Rid, out var resolve))
                    {
                        return resolve;
                    }
                    else
                    {
                        if (models.TryGetValue(rid.Rid, out var model))
                        {
                            var obj = new Dictionary<string, JToken>();
                            foreach (var item in model)
                            {
                                obj[item.Key] = ResolveToken(item.Value, resolved);
                            }

                            return resolved[rid.Rid] = JToken.FromObject(obj);
                        }

                        if (collections.TryGetValue(rid.Rid, out var collection))
                        {
                            var obj = new List<JToken>();
                            foreach (var item in collection)
                            {
                                obj.Add(ResolveToken(item, resolved));
                            }

                            return resolved[rid.Rid] = JToken.FromObject(obj);
                        }
                    }
                }

            }

            return token;
        }

        private JToken ResolveJToken(JToken token)
        {
            Dictionary<string, JToken> resolved = new Dictionary<string, JToken>();
            return ResolveToken(token, resolved);
        }
        private JToken ResolveJToken(Dictionary<string, JToken> tokens)
        {
            Dictionary<string, JToken> resolved = new Dictionary<string, JToken>();
            var ret = new Dictionary<string, JToken>();
            foreach(var token in tokens)
            {
                ret[token.Key] = ResolveToken(token.Value, resolved);
            }
            return JToken.FromObject(ret);
        }
        private JToken ResolveToken(Dictionary<string, JToken> tokens, Dictionary<string, JToken> resolved)
        {
            var ret = new Dictionary<string, JToken>();
            foreach (var token in tokens)
            {
                ret[token.Key] = ResolveToken(token.Value, resolved);
            }
            return JToken.FromObject(ret);
        }

        private void AddNewData(Data data)
        {
            if (data != null)
            {
                if (data.Models != null)
                {
                    foreach (var model in data.Models)
                    {
                        models[model.Key] = model.Value;
                    }
                }

                if (data.Collections != null)
                {
                    foreach (var collection in data.Collections)
                    {
                        collections[collection.Key] = collection.Value;
                    }
                }

                /*
                if (data.Models != null)
                {
                    lock (initialModels)
                    {
                        foreach (var model in data.Models)
                        {
                              initialModels[model.Key] = ResolveJToken(model.Value);
                        }
                    }
                }

                if (data.Collections != null)
                {
                    lock (initialCollections)
                    {
                        foreach (var collection in data.Collections)
                        {
                            initialCollections[collection.Key] = collection.Value.Select(ResolveJToken).ToList();
                        }
                    }
                }
                */
            }
        }

        private void AddNewSubscriptions(Data data)
        {
            if (data != null)
            {
                if (data.Models != null)
                {
                    foreach (var model in data.Models)
                    {
                        models[model.Key] = model.Value;
                        foreach (var item in model.Value)
                        {
                            AddIndirectSubscription(model.Key, item.Value);
                        }
                    }
                }

                if (data.Collections != null)
                {
                    foreach (var collection in data.Collections)
                    {
                        collections[collection.Key] = collection.Value;
                        foreach (var item in collection.Value)
                        {
                            AddIndirectSubscription(collection.Key, item);
                        }
                    }
                }

                if (data.Models != null)
                {
                    foreach (var model in data.Models)
                    {
                        StateInitial?.Invoke(this, new StateInitialEventArgs(model.Key, ResolveJToken(model.Value)));
                    }
                }

                if (data.Collections != null)
                {
                    foreach (var collection in data.Collections)
                    {
                        StateColInitial?.Invoke(this, new StateColInitialEventArgs(collection.Key, collection.Value.Select(ResolveJToken).ToList()));
                    }
                }
            }
        }

        private void ReverseChanged(string rid)
        {
            Dictionary<string, JToken> resolved = new Dictionary<string, JToken>();
            var set = new HashSet<string>();

            var check = new Queue<string>();
            check.Enqueue(rid);

            var prev = new Dictionary<string, string>();

            while (check.Any())
            {
                var item = check.Dequeue();
                if (!set.Contains(item))
                {
                    set.Add(item);
                    if(indirectSubscriptions.TryGetReverse(item, out var values))
                    {
                        foreach (var sub in values)
                        {
                            if (!set.Contains(sub))
                            {
                                prev[sub] = item;
                                check.Enqueue(sub);
                            }
                        }
                    }
                }
            }

            set.Remove(rid);

            foreach (var item in set)
            {
                if (models.TryGetValue(item, out var model))
                {
                    StateChanged?.Invoke(this, new StateChangedEventArgs(item, ResolveToken(model, resolved)));
                }
                if (collections.TryGetValue(item, out var collection))
                {
                    for(int index = 0; index < collection.Count; index++)
                    {
                        if (collection[index].Type == JTokenType.Object)
                        {
                            var r = collection[index].ToObject<ValueRid>();
                            if (r != null && !string.IsNullOrWhiteSpace(r.Rid))
                            {
                                if(prev.TryGetValue(item, out var val) && val == r.Rid)
                                { 
                                    StateColChanged?.Invoke(this, new StateColChangedEventArgs(item, index, ResolveToken(collection[index], resolved)));
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void EventCalled(string data)
        {
            var parsed = JsonConvert.DeserializeObject<EventBase>(data);

            switch (parsed.EventType)
            {
                case "change":
                    {
                        var change = parsed.Data.ToObject<DataChanged>();
                        var item = parsed.Rid;
                        if (models.TryGetValue(item, out var model))
                        {
                            foreach (var val in change.Values)
                            {
                                if (model.TryGetValue(val.Key, out var obj))
                                    RemoveIndirectSubscription(item, obj);
                                if (val.Value.Type == JTokenType.Object)
                                {
                                    var action = val.Value.ToObject<ActionDelete>();
                                    if (action != null && action.Action == "delete")
                                    {
                                        model.Remove(val.Key);
                                    }
                                    else
                                    {
                                        model[val.Key] = val.Value;
                                        AddIndirectSubscription(item, val.Value);
                                    }
                                }
                                else
                                {
                                    model[val.Key] = val.Value;
                                    AddIndirectSubscription(item, val.Value);
                                }
                            }
                        }

                        AddNewSubscriptions(change);
                        StateChanged?.Invoke(this, new StateChangedEventArgs(item, ResolveJToken(model)));
                        ReverseChanged(item);
                    }
                    break;
                case "add":
                    {
                        var add = parsed.Data.ToObject<DataAdded>();
                        var item = parsed.Rid;
                        if (collections.TryGetValue(item, out var collection))
                        {
                            collection.Insert(add.Index, add.Value);
                            AddIndirectSubscription(item, add.Value);
                        }

                        AddNewSubscriptions(add);
                        StateColAdded?.Invoke(this, new StateColAddedEventArgs(item, add.Index, ResolveJToken(add.Value)));
                        ReverseChanged(item);
                    }
                    break;
                case "remove":
                    {
                        var remove = parsed.Data.ToObject<DataRemoved>();
                        var item = parsed.Rid;
                        if (collections.TryGetValue(item, out var collection))
                        {
                            RemoveIndirectSubscription(item, collection[remove.Index]);
                            collection.RemoveAt(remove.Index);
                        }
                        StateColRemoved?.Invoke(this, new StateColRemovedEventArgs(item, remove.Index));
                        ReverseChanged(item);
                    }
                    break;
                case "delete":
                    {
                        var item = parsed.Rid;
                        if (models.TryGetValue(item, out var model))
                        {
                            foreach(var m in model)
                            {
                                RemoveIndirectSubscription(item, m.Value);
                            }
                        }
                        models.Remove(item);
                    }
                    break;
            }
        }

        private sealed class ValueRid
        {
            [JsonProperty("rid")] public string Rid;
        }

        private sealed class ActionDelete
        {
            [JsonProperty("action")] public string Action;
        }

        private sealed class EventBase
        {
            [JsonProperty("event")] public string Event;
            [JsonProperty("data")] public JToken Data;

            public string EventType => Event.Substring(Event.LastIndexOf('.') + 1);
            public string Rid => Event.Substring(0, Event.LastIndexOf('.'));
        }

        private sealed class DataContainer
        {
            [JsonProperty("result")] public Data Result;
        }

        private class Data
        {
            [JsonProperty("models")] public Dictionary<string, Dictionary<string, JToken>> Models;
            [JsonProperty("collections")] public Dictionary<string, List<JToken>> Collections;
            [JsonProperty("errors")] public Dictionary<string, Error> Errors;

            public sealed class Error
            {
                [JsonProperty("code")] public string Code;
                [JsonProperty("message")] public string Message;
                [JsonProperty("data")] public JToken Data;
            }
        }

        private sealed class DataChanged : Data
        {
            public Dictionary<string, JToken> Values;
        }

        private sealed class DataAdded : Data
        {
            [JsonProperty("idx")] public int Index;
            [JsonProperty("value")] public JToken Value;
        }

        private sealed class DataRemoved : Data
        {
            [JsonProperty("idx")] public int Index;
        }
    }
}
