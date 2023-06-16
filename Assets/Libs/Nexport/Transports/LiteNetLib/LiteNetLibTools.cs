using LiteNetLib;

namespace Nexport.Transports.LiteNetLib
{
    public static class LiteNetLibTools
    {
        public static MessageChannel GetMessageChannel(DeliveryMethod channel)
        {
            switch (channel)
            {
                case DeliveryMethod.ReliableOrdered:
                    return MessageChannel.Reliable;
                case DeliveryMethod.ReliableUnordered:
                    return MessageChannel.ReliableUnordered;
                case DeliveryMethod.ReliableSequenced:
                    return MessageChannel.ReliableSequenced;
                case DeliveryMethod.Unreliable:
                    return MessageChannel.Unreliable;
                case DeliveryMethod.Sequenced:
                    return MessageChannel.UnreliableSequenced;
            }
            return MessageChannel.Unknown;
        }

        public static DeliveryMethod GetDeliveryMethod(MessageChannel channel)
        {
            switch (channel)
            {
                case MessageChannel.Reliable:
                    return DeliveryMethod.ReliableOrdered;
                case MessageChannel.Unreliable:
                    return DeliveryMethod.Unreliable;
                case MessageChannel.ReliableSequenced:
                    return DeliveryMethod.ReliableSequenced;
                case MessageChannel.ReliableUnordered:
                    return DeliveryMethod.ReliableUnordered;
                case MessageChannel.UnreliableSequenced:
                    return DeliveryMethod.Unreliable;
            }
            return DeliveryMethod.ReliableOrdered;
        }
    }
}