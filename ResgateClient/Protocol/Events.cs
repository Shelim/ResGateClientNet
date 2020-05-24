using System;
using System.Collections.Generic;
using System.Text;
using Resgate.Resgate;

namespace Resgate.Protocol
{
    public sealed class ErrorEventArgs : EventArgs
    {
        public readonly string Rid;
        public readonly ErrorException Error;

        public ErrorEventArgs(string rid, ErrorException error)
        {
            Rid = rid;
            Error = error;
        }
    }
    public sealed class FailedEventArgs : EventArgs
    {
        public enum FailedReason
        {
            UnsupportedVersion,
            VersionNegotiationFailed
        }
        public readonly FailedReason Reason;
        
        public FailedEventArgs(FailedReason reason)
        {
            Reason = reason;
        }
    }
    public sealed class EventReceivedEventArgs : EventArgs
    {
        public readonly string Msg;

        public EventReceivedEventArgs(string msg)
        {
            Msg = msg;
        }
    }
    public sealed class ConnectionLostEventArgs : EventArgs
    {
    }
    public sealed class ConnectionReestablishedEventArgs : EventArgs
    {
    }
}
