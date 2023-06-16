using System;

namespace Nexport.Transports.Telepathy
{
    public class TelepathyClientIdentifier : ClientIdentifier
    {
        public TelepathyClientIdentifier(int id) => Identifier = id.ToString();
        public int ToConnectionId() => Convert.ToInt32(Identifier);
    }
}