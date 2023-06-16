#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexport.BuiltinMessages;
using Nexport.Transports;

namespace Nexport
{
    public abstract class Client
    {
        public abstract TransportType TransportType { get; }
        public abstract bool IsOpen { get; }

        public Action OnConnect { get; set; } = () => { };
        public Action JoinedServer { get; set; } = () => { };
        public Action<MsgMeta, MessageChannel> OnMessage { get; set; } = (meta, channel) => { };
        public Action OnDisconnect { get; set; } = () => { };

        public Action<ClientIdentifier> OnNetworkedClientConnect { get; set; } = identifier => { };
        public Action<ClientIdentifier> OnNetworkedClientDisconnect { get; set; } = identifier => { };
            
        private List<ClientIdentifier> _clientIdentifiers = new List<ClientIdentifier>();
        public List<ClientIdentifier> ConnectedNetworkedClients => new List<ClientIdentifier>(_clientIdentifiers);

        public ClientIdentifier? LocalClient { get; private set; }
            
        public ClientSettings Settings { get; }

        public Client(ClientSettings settings) => Settings = settings;
            
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Thread? _thread;
        private Task? _task;
        private bool justJoined = true;

        private List<ClientIdentifier> excludeLocalClient(List<ClientIdentifier> list)
        {
            if (LocalClient == null)
                return list;
            List<ClientIdentifier> newClients = new List<ClientIdentifier>();
            foreach (ClientIdentifier clientIdentifier in list)
            {
                if (!clientIdentifier.Compare(LocalClient))
                    newClients.Add(clientIdentifier);
            }
            return newClients;
        }

        private void getJoinedClients(List<ClientIdentifier> recentClients)
        {
            List<ClientIdentifier> leftClients = new List<ClientIdentifier>();
            foreach (ClientIdentifier clientIdentifier in excludeLocalClient(recentClients))
            {
                bool left = true;
                foreach (ClientIdentifier connectedNetworkedClient in ConnectedNetworkedClients)
                {
                    if (clientIdentifier.Compare(connectedNetworkedClient))
                        left = false;
                }
                if(left)
                    leftClients.Add(clientIdentifier);
            }
            foreach (ClientIdentifier clientIdentifier in leftClients)
                OnNetworkedClientConnect.Invoke(clientIdentifier);
        }

        private void getLeftClients(List<ClientIdentifier> recentClients)
        {
            List<ClientIdentifier> joinedClients = new List<ClientIdentifier>();
            foreach (ClientIdentifier connectedNetworkClients in ConnectedNetworkedClients)
            {
                bool joined = true;
                foreach (ClientIdentifier clientIdentifier in excludeLocalClient(recentClients))
                {
                    if (connectedNetworkClients.Compare(clientIdentifier))
                        joined = false;
                }
                if(joined)
                    joinedClients.Add(connectedNetworkClients);
            }
            foreach (ClientIdentifier clientIdentifier in joinedClients)
                OnNetworkedClientDisconnect.Invoke(clientIdentifier);
        }

        private void registerBaseMessages()
        {
            OnMessage += (meta, channel) =>
            {
                switch (meta.DataId)
                {
                    case "Nexport.BuiltinMessages.ServerClientChange":
                    {
                        ServerClientChange serverClientChange =
                            (ServerClientChange) Convert.ChangeType(meta.Data, meta.TypeOfData);
                        if (serverClientChange.LocalClientIdentifier != null)
                            LocalClient = serverClientChange.LocalClientIdentifier;
                        List<ClientIdentifier> clientIdentifiers = serverClientChange.ConnectedClients?.ToList() ??
                                                                   Array.Empty<ClientIdentifier>().ToList();
                        if (!justJoined)
                        {
                            getLeftClients(clientIdentifiers);
                            getJoinedClients(clientIdentifiers);
                        }
                        else
                        {
                            justJoined = false;
                            JoinedServer.Invoke();
                        }
                        _clientIdentifiers = excludeLocalClient(clientIdentifiers);
                        break;
                    }
                }
            };
            OnDisconnect += () => _clientIdentifiers.Clear();
        }

        public void Create(bool closeOnStop = false, Func<byte[]?>? getClosingMessage = null)
        {
            cts = new CancellationTokenSource();
            if (Settings.UseMultithreading)
            {
                _thread = new Thread(() =>
                {
                    RunTask();
                    registerBaseMessages();
                    while (!cts.IsCancellationRequested)
                    {
                        Update();
                        Thread.Sleep(Settings.ThreadUpdate);
                        PostUpdate();
                    }
                    if (!closeOnStop) return;
                    byte[] closingMessage = getClosingMessage?.Invoke() ?? Array.Empty<byte>();
                    Close(closingMessage);
                });
                _thread.Start();
            }
            else
            {
                _task = Task.Factory.StartNew(() =>
                {
                    RunTask();
                    registerBaseMessages();
                    while (!cts.IsCancellationRequested)
                    {
                        Update();
                        Thread.Sleep(Settings.ThreadUpdate);
                        PostUpdate();
                    }
                    if (!closeOnStop) return;
                    byte[] closingMessage = getClosingMessage?.Invoke() ?? Array.Empty<byte>();
                    Close(closingMessage);
                });
            }
        }

        public abstract void RunTask();
            
        public virtual void Update(){}
            
        public virtual void PostUpdate(){}

        public void Stop() => cts.Cancel();
        
        public abstract void Close(byte[]? closingMessage = null);

        public abstract void SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable);
    }

    public class ClientSettings
    {
        public string Ip { get; }
        public int Port { get; }
        public bool UseMultithreading { get; }
        public int ThreadUpdate { get; }
        public bool UseIPV6 { get; }
            
        public ClientSettings(string ip, int port,
            bool useMultithreading = false, int threadUpdateMs = 10, bool useIPV6 = false)
        {
            Ip = ip;
            Port = port;
            UseMultithreading = useMultithreading;
            ThreadUpdate = threadUpdateMs;
            UseIPV6 = useIPV6;
        }
    }
}