#nullable enable
using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using Nexport.BuiltinMessages;

namespace Nexport.Transports.LiteNetLib
{
    public class LiteNetLibServer : Server
    {
        private EventBasedNetListener? _listener;
        private NetManager? _server;
        private readonly Dictionary<LiteNetLibClientIdentifier, NetPeer> connectedClients =
            new Dictionary<LiteNetLibClientIdentifier, NetPeer>();
        private ServerClientManager<LiteNetLibClientIdentifier, NetPeer>? _clientManager;
            
        public LiteNetLibServer(ServerSettings settings) : base(settings){}

        public override TransportType TransportType => TransportType.LiteNetLib;
        public override List<ClientIdentifier> ConnectedClients => new List<ClientIdentifier>(connectedClients.Keys);
        public override bool IsOpen => _server?.IsRunning ?? false;
            
        public override void RunTask()
        {
            _clientManager = new ServerClientManager<LiteNetLibClientIdentifier, NetPeer>(Settings);
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
                    connectionId.Disconnect();
                if (!arg3)
                {
                    connectedClients.Remove(identifier);
                }
                OnDisconnect.Invoke(identifier);
                ServerClientChange serverClientChange = new ServerClientChange(ConnectedClients);
                BroadcastMessage(Msg.Serialize(serverClientChange));
            };
            _listener = new EventBasedNetListener();
            _server = new NetManager(_listener);
            _listener.ConnectionRequestEvent += request => request.Accept();
            _listener.PeerConnectedEvent += peer =>
            {
                LiteNetLibClientIdentifier kcpClientIdentifier = new LiteNetLibClientIdentifier(peer);
                _clientManager.AddClient(kcpClientIdentifier, peer, b =>
                {
                    if (!b)
                        peer.Disconnect();
                });
            };
            _listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
            {
                try
                {
                    byte[] msg = new byte[reader.AvailableBytes];
                    reader.GetBytes(msg, msg.Length);
                    MsgMeta? msgMeta = Msg.GetMeta(msg);
                    if (_clientManager.IsClientWaiting(peer) && msgMeta != null)
                    {
                        try
                        {
                            _clientManager.VerifyWaitingClient(peer, msgMeta, b =>
                            {
                                if (!b)
                                    peer.Disconnect();
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed to Verify Client " + peer.Id + " for reason " + e);
                            peer.Disconnect();
                        }
                    }
                    else if(msgMeta != null)
                    {
                        if (_clientManager.IsClientPresent(peer))
                        {
                            LiteNetLibClientIdentifier? clientIdentifier =
                                _clientManager.GetClientIdentifierFromConnected(peer);
                            if(clientIdentifier != null)
                                OnMessage.Invoke(clientIdentifier, msgMeta, MessageChannel.Unknown);
                        }
                        else
                            peer.Disconnect();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("KCPServer failed to deserialize message from " + peer.Id + " for reason " + e);
                }
            };
            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                _clientManager.ClientDisconnected(peer);
            };
            _server.Start(Settings.Port);
        }

        public override void Update() => _server?.PollEvents();

        public override void Close(byte[]? closingMessage = null)
        {
            if(closingMessage != null)
                BroadcastMessage(closingMessage);
            _server?.Stop();
        }

        public override void SendMessage(ClientIdentifier client, byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            NetPeer? peer = _clientManager?.GetServerLinkFromConnected((LiteNetLibClientIdentifier) client);
            if (peer != null)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put(message);
                DeliveryMethod deliveryMethod = LiteNetLibTools.GetDeliveryMethod(messageChannel);
                peer.Send(writer, deliveryMethod);
            }
        }

        public override void BroadcastMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable,
            ClientIdentifier? excludeClientIdentifier = null)
        {
            foreach (KeyValuePair<LiteNetLibClientIdentifier, NetPeer> keyValuePair in new Dictionary<LiteNetLibClientIdentifier, NetPeer>(
                         connectedClients))
            {
                bool allowSend = excludeClientIdentifier == null;
                if (excludeClientIdentifier != null)
                {
                    if (!keyValuePair.Key.Compare(excludeClientIdentifier))
                        allowSend = true;
                }
                if (allowSend)
                {
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put(message);
                    DeliveryMethod deliveryMethod = LiteNetLibTools.GetDeliveryMethod(messageChannel);
                    keyValuePair.Value.Send(writer, deliveryMethod);
                }
            }
        }

        public override void KickClient(ClientIdentifier client, byte[]? kickMessage = null)
        {
            NetPeer? peer = _clientManager?.GetServerLinkFromConnected((LiteNetLibClientIdentifier) client);
            if (peer != null)
            {
                if (kickMessage != null)
                {
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put(kickMessage);
                    peer.Disconnect(writer);
                }
                else
                    peer.Disconnect();
            }
        }
    }
}