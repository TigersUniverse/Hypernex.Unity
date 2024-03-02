using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Tools;
using Nexport;
using UnityEngine;

namespace Hypernex.Game
{
    public class NetworkSync : MonoBehaviour, IDisposable
    {
        public bool InstanceHostOnly;
        public bool CanSteal;
        public bool AlwaysSync;
        
        [HideInInspector] public bool NetworkSteal;
        public Action<Vector3> OnForce = force => { };
        public Action OnSteal = () => { };

        public string NetworkOwner { get; private set; }
        public SmoothTransform SmoothTransform => smoothTransform ??= new SmoothTransform(transform, false);
        private Coroutine lastCoroutine;
        private Queue<WorldObjectUpdate> msgs = new();
        private CancellationTokenSource cts;
        private Mutex mutex = new();
        private bool isAlwaysStealing;
        private SmoothTransform smoothTransform;

        public bool IsOwned() => !(string.IsNullOrEmpty(NetworkOwner) || NetworkOwner == String.Empty || NetworkOwner == "");
        public bool IsOwnedByLocalPlayer() => NetworkOwner == APIPlayer.APIUser.Id;

        public void Claim()
        {
            if (IsOwned() && !NetworkSteal)
                return;
            Dispose();
            cts = new CancellationTokenSource();
            mutex = new Mutex();
            new Thread(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.IsOpen)
                    {
                        if (mutex.WaitOne())
                        {
                            if (msgs.Count > 0)
                            {
                                for (int i = 0; i < msgs.Count; i++)
                                {
                                    WorldObjectUpdate p = msgs.Dequeue();
                                    byte[] msg = Msg.Serialize(p);
                                    GameInstance.FocusedInstance.SendMessage(typeof(WorldObjectUpdate).FullName, msg,
                                        p.Action is WorldObjectAction.Claim or WorldObjectAction.Unclaim
                                            ? MessageChannel.Reliable
                                            : MessageChannel.Unreliable);
                                }
                            }
                            mutex.ReleaseMutex();
                        }
                    }
                    Thread.Sleep(10);
                }
            }).Start();
            lastCoroutine = StartCoroutine(UpdateNetwork());
        }

        public void Unclaim(Vector3? direction = null, float VelocityAmount = 0f)
        {
            if(!IsOwnedByLocalPlayer())
                return;
            if (AlwaysSync)
            {
                NetworkSteal = true;
                isAlwaysStealing = true;
            }
            else
            {
                QuickInvoke.InvokeActionOnMainThread(new Action(() => NetworkOwner = String.Empty));
                Dispose();
                if (GameInstance.FocusedInstance != null)
                {
                    WorldObjectUpdate worldObjectUpdate = GetWorldObjectUpdate(GameInstance.FocusedInstance);
                    worldObjectUpdate.Action = WorldObjectAction.Unclaim;
                    if (direction != null)
                        worldObjectUpdate.Velocity =
                            NetworkConversionTools.Vector3Tofloat3(direction.Value * (VelocityAmount * 250f));
                    byte[] msg = Msg.Serialize(worldObjectUpdate);
                    GameInstance.FocusedInstance.SendMessage(typeof(WorldObjectUpdate).FullName, msg);
                }
            }
        }

        internal void HandleNetworkUpdate(WorldObjectUpdate worldObjectUpdate)
        {
            QuickInvoke.InvokeActionOnMainThread(new Action(() => UpdateTransform(worldObjectUpdate)));
            NetworkOwner = worldObjectUpdate.Auth.UserId;
            NetworkSteal = worldObjectUpdate.CanBeStolen;
            switch (worldObjectUpdate.Action)
            {
                case WorldObjectAction.Claim:
                    NetworkOwner = worldObjectUpdate.Auth.UserId;
                    NetworkSteal = worldObjectUpdate.CanBeStolen;
                    //UpdateTransform(worldObjectUpdate);
                    break;
                case WorldObjectAction.Update:
                    //UpdateTransform(worldObjectUpdate);
                    break;
                case WorldObjectAction.Unclaim:
                    if(IsOwnedByLocalPlayer())
                        QuickInvoke.InvokeActionOnMainThread(OnSteal);
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    {
                        if(NetworkOwner == worldObjectUpdate.Auth.UserId)
                            Dispose();
                        NetworkOwner = String.Empty;
                    }));
                    /*QuickInvoke.InvokeActionOnMainThread(OnForce,
                        NetworkConversionTools.float3ToVector3(worldObjectUpdate.Velocity));*/
                    //OnForce.Invoke(NetworkConversionTools.float3ToVector3(worldObjectUpdate.Velocity));
                    break;
            }
        }

        private void UpdateTransform(WorldObjectUpdate worldObjectUpdate)
        {
            NetworkOwner = worldObjectUpdate.Auth.UserId;
            NetworkSteal = worldObjectUpdate.CanBeStolen;
            if (transform.parent == null)
            {
                SmoothTransform.SetLocalSpace(false);
            }
            else
            {
                SmoothTransform.SetLocalSpace(true);
            }
            SmoothTransform.Position = NetworkConversionTools.float3ToVector3(worldObjectUpdate.Object.Position);
            SmoothTransform.Rotation = Quaternion.Euler(new Vector3(worldObjectUpdate.Object.Rotation.x,
                worldObjectUpdate.Object.Rotation.y, worldObjectUpdate.Object.Rotation.z));
            SmoothTransform.Scale = NetworkConversionTools.float3ToVector3(worldObjectUpdate.Object.Size);
            OnForce.Invoke(NetworkConversionTools.float3ToVector3(worldObjectUpdate.Velocity));
        }
        
        private WorldObjectUpdate GetWorldObjectUpdate(GameInstance gameInstance)
        {
            Vector3 pos;
            Vector3 ea;
            if (transform.parent == null)
            {
                pos = transform.position;
                ea = transform.eulerAngles;
            }
            else
            {
                pos = transform.localPosition;
                ea = transform.localEulerAngles;
            }
            Transform root = AnimationUtility.GetRootOfChild(transform);
            return new WorldObjectUpdate
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = gameInstance.userIdToken
                },
                Action = GetAction(),
                CanBeStolen = CanSteal || isAlwaysStealing,
                Object = new NetworkedObject
                {
                    ObjectLocation = transform.parent != null ? root.name + '/' + AnimationUtility.CalculateTransformPath(transform, root) : transform.name,
                    Position = NetworkConversionTools.Vector3Tofloat3(pos),
                    Rotation = NetworkConversionTools.QuaternionTofloat4(
                        new Quaternion(ea.x, ea.y, ea.z, 0)),
                    Size = NetworkConversionTools.Vector3Tofloat3(transform.localScale)
                }
            };
        }

        // Not needed for Unclaim
        private WorldObjectAction GetAction()
        {
            if (IsOwnedByLocalPlayer()) 
                return WorldObjectAction.Update;
            QuickInvoke.InvokeActionOnMainThread(new Action(() =>
            {
                NetworkOwner = APIPlayer.APIUser.Id;
                NetworkSteal = CanSteal;
            }));
            return WorldObjectAction.Claim;
        }

        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Vector3 lastScale;

        private IEnumerator UpdateNetwork()
        {
            while (true)
            {
                bool avoidSync = lastPosition == transform.localPosition && lastRotation == transform.localRotation &&
                                 lastScale == transform.localScale;
                lastPosition = transform.localPosition;
                lastRotation = transform.localRotation;
                lastScale = transform.localScale;
                if (avoidSync)
                    yield return null;
                else
                {
                    if (GameInstance.FocusedInstance is {IsOpen: true})
                    {
                        // TODO: Safe Handle has been closed
                        // This is also causing issues with AlwaysSync not working after being picked up once
                        if (mutex.WaitOne(1))
                        {
                            msgs.Enqueue(GetWorldObjectUpdate(GameInstance.FocusedInstance));
                            mutex.ReleaseMutex();
                        }
                    }
                    else if (msgs.Count > 0)
                        msgs.Clear();

                    yield return new WaitForSeconds(0.05f);
                }
            }
        }

        private void Update()
        {
            SmoothTransform.PullFromTransform = IsOwnedByLocalPlayer();
            SmoothTransform.Update();
        }

        public void Dispose()
        {
            isAlwaysStealing = false;
            msgs.Clear();
            if(cts != null)
            {
                if(!cts.IsCancellationRequested)
                    cts.Cancel();
                cts.Dispose();
            }
            mutex?.Dispose();
            if(lastCoroutine != null)
                StopCoroutine(lastCoroutine);
        }
    }
}