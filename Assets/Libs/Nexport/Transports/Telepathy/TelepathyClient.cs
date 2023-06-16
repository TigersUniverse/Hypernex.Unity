#nullable enable
using System;

namespace Nexport.Transports.Telepathy
{
    public class TelepathyClient : Client
    {
        private global::Telepathy.Client? _client;
        
        public TelepathyClient(ClientSettings settings) : base(settings){}

        public override TransportType TransportType => TransportType.Telepathy;
        public override bool IsOpen => _client?.Connected ?? false;
        
        public override void RunTask()
        {
            _client = new global::Telepathy.Client(4096);
            _client.OnConnected += OnConnect;
            _client.OnData += bytes =>
            {
                try
                {
                    byte[] data = bytes.ToArray();
                    MsgMeta? meta = Msg.GetMeta(data);
                    if(meta != null)
                        OnMessage.Invoke(meta, MessageChannel.Unknown);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to read message for reason " + e);
                }
            };
            _client.OnDisconnected += OnDisconnect;
            _client.Connect(Settings.Ip, Settings.Port);
        }

        public override void Update() => _client?.Tick(100);

        public override void Close(byte[]? closingMessage = null)
        {
            _client?.Disconnect();
        }

        public override void SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            if(IsOpen)
                _client?.Send(new ArraySegment<byte>(message));
        }
    }
}