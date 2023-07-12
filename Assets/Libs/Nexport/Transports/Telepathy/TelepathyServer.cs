#nullable enable
using System;
using System.Collections.Generic;
using Nexport.BuiltinMessages;

namespace Nexport.Transports.Telepathy
{
    public class TelepathyServer : Server
    {
        private global::Telepathy.Server? _server;
        private readonly Dictionary<TelepathyClientIdentifier, int> connectedClients =
            new Dictionary<TelepathyClientIdentifier, int>();
        private ServerClientManager<TelepathyClientIdentifier, int>? _clientManager;
            
        public TelepathyServer(ServerSettings settings) : base(settings){}

        public override TransportType TransportType => TransportType.Telepathy;
        public override List<ClientIdentifier> ConnectedClients => new List<ClientIdentifier>(connectedClients.Keys);
        public override bool IsOpen => _server?.Active ?? false;
            
        public override void RunTask()
        {
            _clientManager = new ServerClientManager<TelepathyClientIdentifier, int>(Settings);
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
            _server = new global::Telepathy.Server(4096);
            _server.OnConnected += i =>
            {
                TelepathyClientIdentifier kcpClientIdentifier = new TelepathyClientIdentifier(i);
                _clientManager.AddClient(kcpClientIdentifier, i, b =>
                {
                    if (!b)
                        _server?.Disconnect(i);
                });
            };
            _server.OnData += (i, bytes) =>
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
                            TelepathyClientIdentifier? clientIdentifier = _clientManager?.GetClientIdentifierFromConnected(i);
                            if(clientIdentifier != null)
                                OnMessage.Invoke(clientIdentifier, msgMeta, MessageChannel.Unknown);
                        }
                        else
                            _server?.Disconnect(i);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("KCPServer failed to deserialize message from " + i + " for reason " + e);
                }
            };
            _server.OnDisconnected += i =>
            {
                _clientManager.ClientDisconnected(i);
            };
            _server.Start((ushort) Settings.Port);
        }
            
        public override void Update() => _server?.Tick(100);

        public override void Close(byte[]? closingMessage = null)
        {
            if(closingMessage != null)
                BroadcastMessage(closingMessage);
            _server?.Stop();
        }

        public override void SendMessage(ClientIdentifier client, byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            int id = _clientManager?.GetServerLinkFromConnected((TelepathyClientIdentifier) client) ?? default;
            if (id != default)
                _server?.Send(id, new ArraySegment<byte>(message));
        }

        public override void BroadcastMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable,
            ClientIdentifier? excludeClientIdentifier = null)
        {
            foreach (KeyValuePair<TelepathyClientIdentifier, int> keyValuePair in new Dictionary<TelepathyClientIdentifier, int>(
                         connectedClients))
            {
                bool allowSend = excludeClientIdentifier == null;
                if (excludeClientIdentifier != null)
                {
                    if (!keyValuePair.Key.Compare(excludeClientIdentifier))
                        allowSend = true;
                }
                if(allowSend)
                    _server?.Send(keyValuePair.Value, new ArraySegment<byte>(message));
            }
        }

        public override void KickClient(ClientIdentifier client, byte[]? kickMessage = null)
        {
            int id = _clientManager?.GetServerLinkFromConnected((TelepathyClientIdentifier) client) ?? default;
            if (id != default)
            {
                if(kickMessage != null)
                    _server?.Send(id, new ArraySegment<byte>(kickMessage));
                _server?.Disconnect(id);
            }
        }
    }
}