using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Websocket.Client;
using Websocket.Client.Models;

namespace Resgate.Base
{
    public sealed class DisconnectedEventArgs : EventArgs
    {
        public readonly DisconnectionInfo Info;

        public DisconnectedEventArgs(DisconnectionInfo info)
        {
            Info = info;
        }
    }

    public sealed class ReconnectedEventArgs : EventArgs
    {
        public readonly ReconnectionInfo Info;

        public ReconnectedEventArgs(ReconnectionInfo info)
        {
            Info = info;
        }
    }

    public sealed class MessageReceivedEventArgs : EventArgs
    {
        public readonly ResponseMessage Msg;

        public MessageReceivedEventArgs(ResponseMessage msg)
        {
            Msg = msg;
        }
    }

    public sealed class MessageSentEventArgs : EventArgs
    {
        public readonly string Msg;

        public MessageSentEventArgs(string msg)
        {
            Msg = msg;
        }
    }
}