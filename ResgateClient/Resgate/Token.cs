using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Resgate
{
    public sealed class TokenModel : IDisposable
    {
        private readonly Client client;
        internal readonly string Rid;
        internal readonly Action<JToken> Initial;
        internal readonly Action<JToken> Changed;
        internal TokenModel(Client client, string rid, Action<JToken> initial, Action<JToken> changed)
        {
            this.client = client;
            Rid = rid;
            Initial = initial;
            Changed = changed;
        }
        public void Dispose()
        {
            client.UnsubscribeToken(this);
        }
    }

    public sealed class TokenCollection : IDisposable
    {
        private readonly Client client;
        internal readonly string Rid;
        internal readonly Action<List<JToken>> Initial;
        internal readonly Action<int, JToken> Added;
        internal readonly Action<int, JToken> Changed;
        internal readonly Action<int> Removed;
        internal TokenCollection(Client client, string rid, Action<List<JToken>> initial, Action<int, JToken> added, Action<int, JToken> changed, Action<int> removed)
        {
            this.client = client;
            Rid = rid;
            Initial = initial;
            Added = added;
            Changed = changed;
            Removed = removed;
        }
        public void Dispose()
        {
            client.UnsubscribeToken(this);
        }
    }

    public sealed class TokenReconnected : IDisposable
    {
        private readonly Client client;
        internal readonly Func<Task> Reconnected;

        internal TokenReconnected(Client client, Func<Task> reconnected)
        {
            this.client = client;
            Reconnected = reconnected;
        }

        public void Dispose()
        {
            client.UnsubscribeToken(this);
        }
    }
}
