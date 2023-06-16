using System;
using LiteNetLib;

namespace Nexport.Transports.LiteNetLib
{
    public class LiteNetLibClientIdentifier : ClientIdentifier
    {
        public LiteNetLibClientIdentifier(NetPeer peer) => Identifier = peer.Id.ToString();
        public int ToConnectionId() => Convert.ToInt32(Identifier);
    }
}