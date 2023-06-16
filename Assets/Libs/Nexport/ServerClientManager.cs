#nullable enable
using System;
using System.Collections.Generic;

namespace Nexport
{
    public class ServerClientManager<T1, T2> where T1 : ClientIdentifier
    {
        public Action<T1, T2> ClientWaited = (identifier, serverLink) => { };
        public Action<T1, T2> ClientConnected = (identifier, serverLink) => { };
        public Action<T1, T2, bool, bool> ClientRemoved = (identifier, serverLink, wasWaited, wasManualDisconnect) => { };

        private readonly Dictionary<T1, T2> ConnectedClients = new Dictionary<T1, T2>();
        private readonly Dictionary<T1, T2> WaitingClients = new Dictionary<T1, T2>();
        private ServerSettings _settings;

        public ServerClientManager(ServerSettings settings) => _settings = settings;

        public bool IsClientWaiting(T2 serverLink)
        {
            foreach (KeyValuePair<T1,T2> keyValuePair in new Dictionary<T1, T2>(WaitingClients))
            {
                if (keyValuePair.Value?.Equals(serverLink) ?? false)
                    return true;
            }
            return false;
        }

        public bool IsClientPresent(T2 serverLink)
        {
            foreach (KeyValuePair<T1,T2> keyValuePair in new Dictionary<T1, T2>(ConnectedClients))
            {
                if (keyValuePair.Value?.Equals(serverLink) ?? false)
                    return true;
            }
            return false;
        }

        public T1? GetClientIdentifierFromWaiting(T2 serverLink)
        {
            foreach (KeyValuePair<T1,T2> keyValuePair in new Dictionary<T1, T2>(WaitingClients))
            {
                if (keyValuePair.Value?.Equals(serverLink) ?? false)
                    return keyValuePair.Key;
            }
            return null;
        }
            
        public T1? GetClientIdentifierFromConnected(T2 serverLink)
        {
            foreach (KeyValuePair<T1,T2> keyValuePair in new Dictionary<T1, T2>(ConnectedClients))
            {
                if (keyValuePair.Value?.Equals(serverLink) ?? false)
                    return keyValuePair.Key;
            }
            return null;
        }
            
        public T2? GetServerLinkFromConnected(T1 clientIdentifier)
        {
            foreach (KeyValuePair<T1,T2> keyValuePair in new Dictionary<T1, T2>(ConnectedClients))
            {
                if (keyValuePair.Key.Compare(clientIdentifier))
                    return keyValuePair.Value;
            }
            return default;
        }
            
        public T2? GetServerLinkFromWaiting(T1 clientIdentifier)
        {
            foreach (KeyValuePair<T1, T2?> keyValuePair in new Dictionary<T1, T2?>(WaitingClients))
            {
                if (keyValuePair.Key.Compare(clientIdentifier))
                    return keyValuePair.Value;
            }
            return default;
        }

        public void AddClient(T1 clientIdentifier, T2 serverLink, Action<bool> result, MsgMeta? meta = null)
        {
            if (!IsClientPresent(serverLink) && !IsClientWaiting(serverLink))
            {
                if (_settings.RequireMessageAuth)
                {
                    if (meta != null)
                    {
                        _settings.ValidateMessage.Invoke(clientIdentifier, meta, b =>
                        {
                            if (b)
                            {
                                ConnectedClients.Add(clientIdentifier, serverLink);
                                ClientConnected.Invoke(clientIdentifier, serverLink);
                                result.Invoke(true);
                            }
                            else
                                result.Invoke(false);
                        });
                        return;
                    }
                    else
                    {
                        WaitingClients.Add(clientIdentifier, serverLink);
                        ClientWaited.Invoke(clientIdentifier, serverLink);
                        result.Invoke(true);
                        return;
                    }
                }
                else
                {
                    ConnectedClients.Add(clientIdentifier, serverLink);
                    ClientConnected.Invoke(clientIdentifier, serverLink);
                    result.Invoke(true);
                    return;
                }
            }
            result.Invoke(false);
        }

        public void VerifyWaitingClient(T2 serverLink, MsgMeta meta, Action<bool> result)
        {
            if (!_settings.RequireMessageAuth)
            {
                result.Invoke(false);
                return;
            }
            if (IsClientWaiting(serverLink))
            {
                T1? clientIdentifier = GetClientIdentifierFromWaiting(serverLink);
                if(clientIdentifier != null)
                    _settings.ValidateMessage.Invoke(clientIdentifier, meta, b =>
                    {
                        if (b)
                        {
                            WaitingClients.Remove(clientIdentifier);
                            ConnectedClients.Add(clientIdentifier, serverLink);
                            ClientConnected.Invoke(clientIdentifier, serverLink);
                            result.Invoke(true);
                        }
                        else
                        {
                            WaitingClients.Remove(clientIdentifier);
                            ClientRemoved.Invoke(clientIdentifier, serverLink, true, false);
                            result.Invoke(false);
                        }
                    });
                return;
            }
            result.Invoke(false);
        }

        public void ClientDisconnected(T2 serverLink)
        {
            T1? connectedClient = GetClientIdentifierFromConnected(serverLink);
            if (connectedClient != null)
            {
                ConnectedClients.Remove(connectedClient);
                ClientRemoved.Invoke(connectedClient, serverLink, false, true);
                return;
            }
            T1? connectedWaiting = GetClientIdentifierFromWaiting(serverLink);
            if (connectedWaiting != null)
            {
                WaitingClients.Remove(connectedWaiting);
                ClientRemoved.Invoke(connectedWaiting, serverLink, false, true);
            }
        }
    }
}