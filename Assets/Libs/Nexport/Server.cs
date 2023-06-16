#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexport.Transports;

namespace Nexport
{
    public abstract class Server
    {
        public abstract TransportType TransportType { get; }
        public abstract List<ClientIdentifier> ConnectedClients { get; }
        public abstract bool IsOpen { get; }

        public Action<ClientIdentifier> OnConnect { get; set; } = bytes => { };
        public Action<ClientIdentifier, MsgMeta, MessageChannel> OnMessage { get; set; } = (identifier, messageMeta, channel) => { };
        public Action<ClientIdentifier> OnDisconnect { get; set; } = identifier => { };
            
        public ServerSettings Settings { get; }

        private CancellationTokenSource cts = new CancellationTokenSource();
        private Thread? _thread;
        private Task? _task;

        public Server(ServerSettings settings) => Settings = settings;

        public void Create(bool closeOnStop = false, Func<byte[]?>? getClosingMessage = null)
        {
            cts = new CancellationTokenSource();
            if (Settings.UseMultithreading)
            {
                _thread = new Thread(() =>
                {
                    RunTask();
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
            
        public virtual void Update(){}
        public virtual void PostUpdate(){}

        public void Stop() => cts.Cancel();

        public abstract void RunTask();

        public abstract void Close(byte[]? closingMessage = null);

        public abstract void SendMessage(ClientIdentifier client, byte[] message,
            MessageChannel messageChannel = MessageChannel.Reliable);

        public abstract void BroadcastMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable,
            ClientIdentifier? excludeClientIdentifier = null);
        public abstract void KickClient(ClientIdentifier client, byte[]? kickMessage = null);
    }

    public class ServerSettings
    {
        public string Ip { get; }
        public int Port { get; }
        public bool RequireMessageAuth { get; }

        public Action<ClientIdentifier, MsgMeta, Action<bool>> ValidateMessage { get; set; } =
            (clientIdentifier, meta, callback) => { };
        public bool UseMultithreading { get; }
        public int ThreadUpdate { get; }
        public bool UseIPV6 { get; }

        public ServerSettings(string ip, int port, bool requireMessageAuth = false,
            bool useMultithreading = false, int threadUpdateMs = 10, bool useIPV6 = false)
        {
            Ip = ip;
            Port = port;
            RequireMessageAuth = requireMessageAuth;
            UseMultithreading = useMultithreading;
            ThreadUpdate = threadUpdateMs;
            UseIPV6 = useIPV6;
        }
    }
}