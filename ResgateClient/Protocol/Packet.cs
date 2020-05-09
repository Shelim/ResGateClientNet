using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Resgate.Utility;

namespace Resgate.Protocol
{
    public sealed class Packet
    {
        public readonly int Id;

        public readonly string Type;
        public readonly string ResourceId;
        public readonly string Method;

        public readonly object Params;

        public readonly string Content;

        public readonly ManualResetEventAsync Handled;

        public string Response;
        public Exception Exception;

        public void ResponseReceived(string response)
        {
            Response = response;
            Handled.Set();
        }
        public void Failed(Exception exception)
        {
            Exception = exception ?? new Exception("Generic network failure");
            Handled.Set();
        }

        public override string ToString()
        {
            return Content;
        }

        public Packet(int id, string type, string resourceId = null, string method = null, object param = null)
        {
            Id = id;
            Type = type;
            ResourceId = resourceId;
            Method = method;
            Params = param;
            Handled = new ManualResetEventAsync(false);

            var m = new StringBuilder();
            m.Append(type);
            if (!string.IsNullOrWhiteSpace(resourceId))
            {
                m.Append(".");
                m.Append(resourceId);
            }

            if (!string.IsNullOrWhiteSpace(method))
            {
                m.Append(".");
                m.Append(method);
            }

            var obj = new Dictionary<string, object>
                {
                    {"id", id},
                    {"method", m.ToString()}
                };
            if (param != null)
            {
                obj["params"] = param;
            }

            Content = JsonConvert.SerializeObject(obj, Formatting.None);
        }
    }
}
