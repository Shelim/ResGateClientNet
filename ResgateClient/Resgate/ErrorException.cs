using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Resgate.Resgate
{
    public sealed class ErrorException : Exception
    {
        public readonly string Code;
        public readonly string Message;
        public readonly JToken DataJson;

        public ErrorException(string code, string message, JToken dataJson)
        {
            Code = code;
            Message = message;
            DataJson = dataJson;
        }

        public string GetDataString(Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(DataJson, formatting);
        }

        public T GetDataObject<T>()
        {
            return DataJson.ToObject<T>();
        }
    }
}
