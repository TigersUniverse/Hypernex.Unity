using kcp2k;

namespace Nexport.Transports.kcp2k
{
    public static class KCPTools
    {
        public static MessageChannel GetMessageChannel(KcpChannel channel)
        {
            switch (channel)
            {
                case KcpChannel.Reliable:
                    return MessageChannel.Reliable;
                case KcpChannel.Unreliable:
                    return MessageChannel.Unreliable;
            }
            return MessageChannel.Unknown;
        }

        public static KcpChannel GetKcpChannel(MessageChannel channel)
        {
            switch (channel)
            {
                case MessageChannel.Reliable:
                    return KcpChannel.Reliable;
                case MessageChannel.Unreliable:
                    return KcpChannel.Unreliable;
                case MessageChannel.ReliableSequenced:
                    return KcpChannel.Reliable;
                case MessageChannel.ReliableUnordered:
                    return KcpChannel.Reliable;
                case MessageChannel.UnreliableSequenced:
                    return KcpChannel.Unreliable;
            }
            return KcpChannel.Reliable;
        }
    }
}