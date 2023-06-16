using Nexport;

namespace Nexport
{
    [Msg]
    public class ClientIdentifier
    {
        [MsgKey(1)] public string MessageId => typeof(ClientIdentifier).FullName;
        [MsgKey(2)] public string Identifier { get; set; }

        public virtual bool Compare(ClientIdentifier identifier) => string.Equals(Identifier, identifier.Identifier);
    }
}