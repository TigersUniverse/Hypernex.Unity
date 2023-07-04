#nullable enable
using System;
using kcp2k;

namespace Nexport.Transports.kcp2k
{
    public class KCPClient : Client
    {
        private KcpClient? _client;
            
        public KCPClient(ClientSettings settings) : base(settings){}
    
        public override TransportType TransportType => TransportType.KCP;
        public override bool IsOpen => _client?.connected ?? false;
            
        public override void RunTask()
        {
            _client = new KcpClient(OnConnect, (bytes, channel) =>
            {
                try
                {
                    byte[] data = bytes.ToArray();
                    MsgMeta? meta = Msg.GetMeta(data);
                    if(meta != null)
                        OnMessage.Invoke(meta, KCPTools.GetMessageChannel(channel));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to read message for reason " + e);
                }
            }, OnDisconnect, (code, s) =>
            {
                Console.WriteLine("TelepathyClient error " + code + " for reason " + s);
            }, new KcpConfig
            {
                DualMode = Settings.UseIPV6,
                RecvBufferSize = 6144000,
                SendBufferSize = 6144000,
                SendWindowSize = 8192,
                ReceiveWindowSize = 8192,
                Interval = 10,
                NoDelay = true,
                CongestionWindow = false,
                MaxRetransmits = Kcp.DEADLINK * 2
            });
            _client.Connect(Settings.Ip, (ushort) Settings.Port);
        }
            
        public override void Update() => _client?.Tick();
    
        public override void Close(byte[]? closingMessage = null)
        {
            _client?.Disconnect();
        }
    
        public override void SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            if(IsOpen)
                _client?.Send(new ArraySegment<byte>(message), KCPTools.GetKcpChannel(messageChannel));
        }
    }
}