using System;

namespace Nexport.Transports.kcp2k
{
    public class KCPClientIdentifier : ClientIdentifier
    {
        public KCPClientIdentifier(int id) => Identifier = id.ToString();
        public int ToConnectionId() => Convert.ToInt32(Identifier);
    }
}