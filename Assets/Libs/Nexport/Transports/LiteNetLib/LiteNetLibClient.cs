#nullable enable
using System;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Nexport.Transports.LiteNetLib
{
    public class LiteNetLibClient : Client
    {
        private EventBasedNetListener? _listener;
        private NetManager? _client;
            
        public LiteNetLibClient(ClientSettings settings) : base(settings){}

        public override TransportType TransportType => TransportType.LiteNetLib;
        public override bool IsOpen => _client?.IsRunning ?? false;
            
        public override void RunTask()
        {
            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);
            _listener.PeerConnectedEvent += peer => OnConnect.Invoke();
            _listener.NetworkReceiveEvent += (fromPeer, dataReader, channel, deliveryMethod) =>
            {
                try
                {
                    byte[] data = new byte[dataReader.AvailableBytes];
                    dataReader.GetBytes(data, data.Length);
                    MsgMeta? meta = Msg.GetMeta(data);
                    if(meta != null)
                        OnMessage.Invoke(meta, MessageChannel.Unknown);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to read message for reason " + e);
                }
            };
            _listener.PeerDisconnectedEvent += (peer, info) => OnDisconnect.Invoke();
            _client.Start();
            _client.Connect(Settings.Ip, Settings.Port, String.Empty);
        }

        public override void Update() => _client?.PollEvents();

        public override void Close(byte[]? closingMessage = null)
        {
            _client?.Stop();
        }

        public override void SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            if (IsOpen)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put(message);
                DeliveryMethod deliveryMethod = LiteNetLibTools.GetDeliveryMethod(messageChannel);
                _client?.FirstPeer.Send(writer, deliveryMethod);
            }
        }
    }
}