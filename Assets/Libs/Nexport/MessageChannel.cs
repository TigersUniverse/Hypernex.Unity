namespace Nexport
{
    public enum MessageChannel
    {
        // aka ReliableOrdered
        Reliable,
        ReliableUnordered,
        ReliableSequenced,
        Unreliable,
        UnreliableSequenced,
        Unknown
    }
}