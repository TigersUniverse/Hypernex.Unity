using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hypernex.ExtendedTracking;
using Hypernex.Game.Avatar;
using Hypernex.Game.Bindings;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Bulk;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Tools;
using Nexport;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Game.Networking
{
    public class LocalPlayerSyncController : IDisposable
    {
        private const float MESSAGE_UPDATE_TIME = 0.05f;

        internal static string CalibrationData = null;
        internal static bool calibratedFBT;
        
        private LocalPlayer localPlayer;
        private int lastPlayerCount;
        private int playerCountLoop;
        private bool forceUpdate;
        private string lastAvatarId;
        
        public List<string> LastPlayerAssignedTags = new();
        public Dictionary<string, object> LastExtraneousObjects = new();
        
        private CancellationTokenSource cts = new();
        private Mutex mutex = new();
        private Queue<BulkWeightedObjectUpdate> woumsgs = new();
        
        private List<WeightedObjectUpdate> weightedObjectUpdates = new();
        private PlayerUpdate lastPlayerUpdate;
        private PlayerDataUpdate lastPlayerDataUpdate;
        
        public LocalPlayerSyncController(LocalPlayer localPlayer, Action<IEnumerator> c)
        {
            this.localPlayer = localPlayer;
            new Thread(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.IsOpen)
                    {
                        if (mutex.WaitOne())
                        {
                            if (woumsgs.Count > 0)
                            {
                                for (int i = 0; i < woumsgs.Count; i++)
                                {
                                    BulkWeightedObjectUpdate w = woumsgs.Dequeue();
                                    byte[] msg = Msg.Serialize(w);
                                    GameInstance.FocusedInstance.SendMessage(typeof(BulkWeightedObjectUpdate).FullName, msg,
                                        MessageChannel.UnreliableSequenced);
                                }
                            }
                            mutex.ReleaseMutex();
                        }
                    }
                    Thread.Sleep(10);
                }
            }).Start();
            c.Invoke(UpdatePlayer());
        }
        
        private IEnumerator UpdatePlayer()
        {
            while (!cts.IsCancellationRequested)
            {
                GameInstance gameInstance = GameInstance.FocusedInstance;
                if(gameInstance != null && gameInstance.IsOpen)
                {
                    if (lastPlayerCount != gameInstance.ConnectedUsers.Count)
                    {
                        if (lastPlayerCount > gameInstance.ConnectedUsers.Count)
                        {
                            lastPlayerCount = gameInstance.ConnectedUsers.Count;
                            playerCountLoop = 0;
                            forceUpdate = false;
                        }
                        else if (lastPlayerCount != gameInstance.ConnectedUsers.Count && playerCountLoop < 5)
                        {
                            forceUpdate = true;
                            playerCountLoop++;
                            if (playerCountLoop >= 5)
                            {
                                lastPlayerCount = gameInstance.ConnectedUsers.Count;
                                playerCountLoop = 0;
                                forceUpdate = false;
                            }
                        }
                    }
                    else
                        forceUpdate = false;
                    PlayerUpdate playerUpdate = GetPlayerUpdate();
                    if (!string.IsNullOrEmpty(CalibrationData))
                    {
                        playerUpdate.IsFBT = calibratedFBT;
                        playerUpdate.VRIKJson = CalibrationData;
                    }
                    if (!playerUpdate.Equals(lastPlayerUpdate) || forceUpdate)
                        gameInstance.SendMessage(typeof(PlayerUpdate).FullName, Msg.Serialize(playerUpdate),
                            MessageChannel.UnreliableSequenced);
                    PlayerDataUpdate playerDataUpdate = GetPlayerDataUpdate();
                    if (!playerDataUpdate.Equals(lastPlayerDataUpdate) || forceUpdate)
                        gameInstance.SendMessage(typeof(PlayerDataUpdate).FullName,
                            Msg.Serialize(playerDataUpdate),
                            MessageChannel.UnreliableSequenced);
                    lastPlayerUpdate = playerUpdate;
                    lastPlayerDataUpdate = playerDataUpdate;
                    PlayerObjectUpdate playerObjectUpdate = new PlayerObjectUpdate
                    {
                        Auth = new JoinAuth
                        {
                            UserId = APIPlayer.APIUser.Id,
                            TempToken = gameInstance.userIdToken
                        },
                        Objects = GetCoreTransforms()
                    };
                    gameInstance.SendMessage(typeof(PlayerObjectUpdate).FullName, Msg.Serialize(playerObjectUpdate),
                        MessageChannel.Unreliable);
                    WeightCheck();
                }
                yield return new WaitForSeconds(MESSAGE_UPDATE_TIME);
            }
        }

        private CoreBone TrackerRoleToCoreBone(XRTrackerRole xrTrackerRole)
        {
            switch (xrTrackerRole)
            {
                case XRTrackerRole.Hip:
                    return CoreBone.Hip;
                case XRTrackerRole.LeftFoot:
                    return CoreBone.LeftFoot;
                case XRTrackerRole.RightFoot:
                    return CoreBone.RightFoot;
            }
            return CoreBone.Camera;
        }

        private Dictionary<int, NetworkedObject> GetCoreTransforms()
        {
            Dictionary<int, NetworkedObject> coreTransforms = new Dictionary<int, NetworkedObject>();
            coreTransforms.Add((int) CoreBone.Root, localPlayer.transform.GetNetworkTransform());
            coreTransforms.Add((int) CoreBone.Head,
                localPlayer.Camera.transform.GetNetworkTransform(localPlayer.transform));
            if (LocalPlayer.IsVR)
            {
                coreTransforms.Add((int) CoreBone.LeftHand,
                    localPlayer.LeftHandVRIKTarget.GetNetworkTransform(localPlayer.transform));
                coreTransforms.Add((int) CoreBone.RightHand,
                    localPlayer.RightHandVRIKTarget.GetNetworkTransform(localPlayer.transform));
                if (XRTracker.CanFBT)
                {
                    foreach (XRTracker tracker in XRTracker.Trackers)
                    {
                        XRTrackerRole xrTrackerRole = tracker.TrackerRole;
                        if(xrTrackerRole == XRTrackerRole.Camera) continue;
                        Transform vriktarget = tracker.transform.GetChild(0);
                        coreTransforms.Add((int) TrackerRoleToCoreBone(xrTrackerRole),
                            vriktarget.GetNetworkTransform(localPlayer.transform));
                    }
                }
            }
            foreach (HandleCamera handleCamera in HandleCamera.allCameras)
            {
                NetworkedObject networkedObject = handleCamera.transform.GetNetworkTransform();
                networkedObject.IgnoreObjectLocation = true;
                networkedObject.ObjectLocation = "*" + handleCamera.gameObject.name;
                coreTransforms.Add((int) CoreBone.Camera, networkedObject);
            }
            return coreTransforms;
        }
        
        private void AddSystemTags(ref List<string> tags)
        {
            if (!tags.Contains("*eyetracking") && FaceTrackingManager.EyeTracking)
                tags.Add("*eyetracking");
            if (!tags.Contains("*liptracking") && FaceTrackingManager.LipTracking)
                tags.Add("*liptracking");
        }
        
        private void TagsCheck(ref List<string> tags)
        {
            if (tags.Contains("*eyetracking") && !FaceTrackingManager.EyeTracking)
                tags.Remove("*eyetracking");
            if (tags.Contains("*liptracking") && !FaceTrackingManager.LipTracking)
                tags.Remove("*liptracking");
        }
        
        private PlayerUpdate GetPlayerUpdate()
        {
            if (GameInstance.FocusedInstance == null || !GameInstance.FocusedInstance.IsOpen)
                return null;
            PlayerUpdate playerUpdate = new PlayerUpdate
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = GameInstance.FocusedInstance.userIdToken
                },
                IsPlayerVR = LocalPlayer.IsVR,
                IsSpeaking = localPlayer.MicrophoneEnabled
            };
            if (localPlayer.avatarMeta != null)
                playerUpdate.AvatarId = localPlayer.avatarMeta.Id;
            return playerUpdate;
        }

        private PlayerDataUpdate GetPlayerDataUpdate()
        {
            if (GameInstance.FocusedInstance == null || !GameInstance.FocusedInstance.IsOpen)
                return null;
            PlayerDataUpdate playerDataUpdate = new PlayerDataUpdate
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = GameInstance.FocusedInstance.userIdToken
                },
                PlayerAssignedTags = new List<string>(),
                ExtraneousData = new Dictionary<string, object>()
            };
            AddSystemTags(ref playerDataUpdate.PlayerAssignedTags);
            foreach (string s in new List<string>(LocalPlayer.MorePlayerAssignedTags))
                if(!playerDataUpdate.PlayerAssignedTags.Contains(s))
                    playerDataUpdate.PlayerAssignedTags.Add(s);
            foreach (KeyValuePair<string,object> extraneousObject in new Dictionary<string, object>(LocalPlayer.MoreExtraneousObjects))
                if(!playerDataUpdate.ExtraneousData.ContainsKey(extraneousObject.Key))
                    playerDataUpdate.ExtraneousData.Add(extraneousObject.Key, extraneousObject.Value);
            TagsCheck(ref playerDataUpdate.PlayerAssignedTags);
            LastPlayerAssignedTags = new List<string>(playerDataUpdate.PlayerAssignedTags);
            LastExtraneousObjects = new Dictionary<string, object>(playerDataUpdate.ExtraneousData);
            return playerDataUpdate;
        }
        
        public void ResetWeights() => weightedObjectUpdates.Clear();

        public void UpdateWeights(List<WeightedObjectUpdate> wos, bool reset)
        {
            if (GameInstance.FocusedInstance == null || !GameInstance.FocusedInstance.IsOpen) return;
            if (mutex.WaitOne(1))
            {
                BulkWeightedObjectUpdate bulkWeightedObjectUpdate = new BulkWeightedObjectUpdate
                {
                    Auth = new JoinAuth
                    {
                        UserId = APIPlayer.APIUser.Id,
                        TempToken = GameInstance.FocusedInstance.userIdToken
                    },
                    WeightedObjectUpdates = wos.ToArray(),
                    Reset = reset
                };
                woumsgs.Enqueue(bulkWeightedObjectUpdate);
                mutex.ReleaseMutex();
            }
        }
        
        private void WeightCheck()
        {
            if (APIPlayer.APIUser == null || GameInstance.FocusedInstance == null) return;
            // These are going into a Bulk, so we shouldn't include any token
            List<WeightedObjectUpdate> w = localPlayer.avatar?.GetAnimatorWeights();
            if(w == null) return;
            if (LocalPlayer.IsVR)
            {
                XRBinding left = null;
                XRBinding right = null;
                foreach (IBinding binding in localPlayer.Bindings)
                    switch (binding.Id)
                    {
                        case "Left VRController":
                            left = (XRBinding) binding;
                            break;
                        case "Right VRController":
                            right = (XRBinding) binding;
                            break;
                    }

                if (left != null && right != null)
                {
                    List<(string, float)> fingerTrackingWeights = XRBinding.GetFingerTrackingWeights(left, right);
                    foreach ((string, float) fingerTrackingWeight in fingerTrackingWeights)
                    {
                        WeightedObjectUpdate weightedObjectUpdate = new WeightedObjectUpdate
                        {
                            TypeOfWeight = AvatarCreator.PARAMETER_ID,
                            PathToWeightContainer = AvatarCreator.ALL_ANIMATOR_LAYERS,
                            WeightIndex = fingerTrackingWeight.Item1,
                            Weight = fingerTrackingWeight.Item2
                        };
                        w.RemoveAll(x => x.WeightIndex == fingerTrackingWeight.Item1);
                        w.Add(weightedObjectUpdate);
                    }
                }
            }
            if (w.Count != weightedObjectUpdates.Count || lastAvatarId != localPlayer.avatarMeta?.Id)
            {
                weightedObjectUpdates.Clear();
                w.ForEach(x => weightedObjectUpdates.Add(x));
                UpdateWeights(weightedObjectUpdates, true);
            }
            else
            {
                List<WeightedObjectUpdate> wos = new List<WeightedObjectUpdate>();
                for (int x = 0; x < w.Count; x++)
                {
                    WeightedObjectUpdate recent = w.ElementAt(x);
                    try
                    {
                        WeightedObjectUpdate cached = weightedObjectUpdates.First(b =>
                            b.TypeOfWeight == recent.TypeOfWeight &&
                            b.PathToWeightContainer == recent.PathToWeightContainer &&
                            b.WeightIndex == recent.WeightIndex);
                        if (recent.Weight != cached.Weight || forceUpdate)
                        {
                            int y = weightedObjectUpdates.IndexOf(cached);
                            weightedObjectUpdates[y] = recent;
                            wos.Add(recent);
                        }
                    }
                    catch(Exception){}
                }
                if(wos.Count > 0) UpdateWeights(wos, false);
            }
            if(localPlayer.avatarMeta != null)
                lastAvatarId = localPlayer.avatarMeta?.Id;
        }

        public void Dispose()
        {
            cts?.Dispose();
            mutex?.Dispose();
        }
    }
}