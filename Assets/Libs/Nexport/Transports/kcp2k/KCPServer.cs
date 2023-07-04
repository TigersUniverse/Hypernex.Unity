#nullable enable
using System;
using System.Collections.Generic;
using kcp2k;
using Nexport.BuiltinMessages;

namespace Nexport.Transports.kcp2k
{
    public class KCPServer : Server
    {
        private KcpServer? _server;

        private readonly Dictionary<KCPClientIdentifier, int> connectedClients =
            new Dictionary<KCPClientIdentifier, int>();
        private ServerClientManager<KCPClientIdentifier, int>? _clientManager;

        public KCPServer(ServerSettings settings) : base(settings){}

        public override TransportType TransportType => TransportType.KCP;
        public override List<ClientIdentifier> ConnectedClients => new List<ClientIdentifier>(connectedClients.Keys);
        public override bool IsOpen => _server?.IsActive() ?? false;

        public override void RunTask()
        {
            _clientManager = new ServerClientManager<KCPClientIdentifier, int>(Settings);
            _clientManager.ClientConnected += (identifier, connectionId) =>
            {
                connectedClients.Add(identifier, connectionId);
                OnConnect.Invoke(identifier);
                ServerClientChange serverClientChange = new ServerClientChange(ConnectedClients);
                BroadcastMessage(Msg.Serialize(serverClientChange), excludeClientIdentifier: identifier);
                serverClientChange.LocalClientIdentifier = identifier;
                SendMessage(identifier, Msg.Serialize(serverClientChange));
            };
            _clientManager.ClientRemoved += (identifier, connectionId, arg3, arg4) =>
            {
                if(!arg4)
                    _server?.Disconnect(connectionId);
                if (!arg3)
                {
                    connectedClients.Remove(identifier);
                }
                OnDisconnect.Invoke(identifier);
                ServerClientChange serverClientChange = new ServerClientChange(ConnectedClients);
                BroadcastMessage(Msg.Serialize(serverClientChange));
            };
            _server = new KcpServer(i =>
            {
                KCPClientIdentifier kcpClientIdentifier = new KCPClientIdentifier(i);
                _clientManager.AddClient(kcpClientIdentifier, i, b =>
                {
                    if (!b)
                        _server?.Disconnect(i);
                });
            }, (i, bytes, channel) =>
            {
                try
                {
                    byte[] msg = bytes.ToArray();
                    MsgMeta? msgMeta = Msg.GetMeta(msg);
                    if (_clientManager.IsClientWaiting(i) && msgMeta != null)
                    {
                        try
                        {
                            _clientManager.VerifyWaitingClient(i, msgMeta, b =>
                            {
                                if (!b)
                                    _server?.Disconnect(i);
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed to Verify Client " + i + " for reason " + e);
                            _server?.Disconnect(i);
                        }
                    }
                    else if(msgMeta != null)
                    {
                        if (_clientManager.IsClientPresent(i))
                        {
                            KCPClientIdentifier? clientIdentifier = _clientManager.GetClientIdentifierFromConnected(i);
                            if(clientIdentifier != null)
                                OnMessage.Invoke(clientIdentifier, msgMeta, KCPTools.GetMessageChannel(channel));
                        }
                        else
                            _server?.Disconnect(i);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("KCPServer failed to deserialize message from " + i + " for reason " + e);
                }
            }, i => _clientManager.ClientDisconnected(i), (i, code, arg3) =>
            {
                _clientManager.ClientDisconnected(i);
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
            _server.Start((ushort) Settings.Port);
        }

        public override void Update() => _server?.Tick();

        public override void Close(byte[]? closingMessage = null)
        {
            if(closingMessage != null)
                BroadcastMessage(closingMessage);
            _server?.Stop();
        }

        public override void SendMessage(ClientIdentifier client, byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            int id = _clientManager?.GetServerLinkFromConnected((KCPClientIdentifier) client) ?? default;
            if (id != default)
                _server?.Send(id, new ArraySegment<byte>(message), KCPTools.GetKcpChannel(messageChannel));
        }

        public override void BroadcastMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable,
            ClientIdentifier? excludeClientIdentifier = null)
        {
            foreach (KeyValuePair<KCPClientIdentifier, int> keyValuePair in new Dictionary<KCPClientIdentifier, int>(
                         connectedClients))
            {
                bool allowSend = excludeClientIdentifier == null;
                if (excludeClientIdentifier != null)
                {
                    if (!keyValuePair.Key.Compare(excludeClientIdentifier))
                        allowSend = true;
                }
                if(allowSend)
                    _server?.Send(keyValuePair.Value, new ArraySegment<byte>(message),
                        KCPTools.GetKcpChannel(messageChannel));
            }
        }

        public override void KickClient(ClientIdentifier client, byte[]? kickMessage = null)
        {
            int id = _clientManager?.GetServerLinkFromConnected((KCPClientIdentifier) client) ?? default;
            if (id != default)
            {
                if(kickMessage != null)
                    _server?.Send(id, new ArraySegment<byte>(kickMessage), KcpChannel.Reliable);
                _server?.Disconnect(id);
            }
        }
    }
}