using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.CCK;
using Hypernex.CCK.Unity;
using Hypernex.Configuration;
using Hypernex.Game.Audio;
using Hypernex.Game.Avatar;
using Hypernex.Game.Networking;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Bulk;
using Hypernex.Networking.Messages.Data;
using Hypernex.Player;
using Hypernex.Sandboxing;
using Hypernex.Tools;
using Hypernex.UI.Templates;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketMessages;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Game
{
    public class NetPlayer : MonoBehaviour
    {
        [HideInInspector] public string UserId;
        private GameInstance instance;
        private Scene scene;
        public User User;
        public NetAvatarCreator Avatar;

        private PlayerUpdate lastPlayerUpdate;
        private bool lastVR;
        private string lastVRIKjson;
        private VRIKCalibrator.CalibrationData lastCalibrationData;
        private string CalibratedAvatarId;
        private string AvatarId;
        private SharedAvatarToken avatarFileToken;
        private AvatarMeta avatarMeta;
        private Builds avatarBuild;
        internal NameplateTemplate nameplateTemplate;

        public float interpolationFramesCount = 0.1f;
        private int elapsedFrames;

        internal Dictionary<CoreBone, SmoothTransform> smoothTransforms = new();

        public float volume
        {
            get
            {
                if (ConfigManager.SelectedConfigUser == null)
                    return 1.0f;
                if (!ConfigManager.SelectedConfigUser.UserVolumes.ContainsKey(UserId))
                    return 1.0f;
                return ConfigManager.SelectedConfigUser.UserVolumes[UserId];
            }
        }
        private AudioClip voice;

        [HideInInspector] public List<string> LastPlayerTags = new();
        public Dictionary<string, object> LastExtraneousObjects = new();

        internal Transform GetReferenceFromCoreBone(CoreBone coreBone)
        {
            if (coreBone == CoreBone.Root) return transform;
            Transform t = null;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name == coreBone.ToString())
                {
                    t = child;
                    break;
                }
            }
            if (t == null)
            {
                GameObject newReference = new GameObject(coreBone.ToString());
                t = newReference.transform;
                t.SetParent(transform);
                t.SetPositionAndRotation(Vector3.zero, new Quaternion(0,0,0, 0));
            }
            return t;
        }

        private void CreateNameplate()
        {
            GameObject np = Instantiate(DontDestroyMe.GetNotDestroyedObject("UXTemplates").transform.Find("Nameplate")
                .gameObject);
            SceneManager.MoveGameObjectToScene(np, instance.loadedScene);
            np.transform.SetParent(transform);
            np.gameObject.SetActive(true);
            nameplateTemplate = np.GetComponent<NameplateTemplate>();
            nameplateTemplate.np = this;
            nameplateTemplate.Render(User);
            if(Avatar != null)
                nameplateTemplate.OnNewAvatar(Avatar);
        }

        private void OnUser(CallbackResult<GetUserResult> result)
        {
            if (!result.success)
            {
                APIPlayer.APIObject.GetUser(OnUser, UserId, isUserId: true);
                return;
            }

            QuickInvoke.InvokeActionOnMainThread(new Action(() =>
            {
                User = result.result.UserData;
                CreateNameplate();
            }));
        }

        private void OnAvatarDownload(Stream stream)
        {
            waitingForAvatarToken = false;
            if (stream == Stream.Null)
            {
                if (avatarFileToken == null)
                    APIPlayer.APIObject.GetFile(OnAvatarDownload, avatarMeta.OwnerId, avatarBuild.FileId);
                else
                    APIPlayer.APIObject.GetFile(OnAvatarDownload, avatarMeta.OwnerId, avatarBuild.FileId,
                        avatarFileToken.avatarToken);
                return;
            }

            QuickInvoke.InvokeActionOnMainThread(new Action(() =>
            {
                string path = Path.Combine(DownloadTools.DownloadsPath, $"{AvatarId}.hna");
                if (File.Exists(path))
                    File.Delete(path);
                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                           FileShare.ReadWrite | FileShare.Delete))
                {
                    stream.CopyTo(fs);
                    StartCoroutine(AssetBundleTools.LoadAvatarFromFile(path, a =>
                    {
                        if (a == null)
                            return;
                        Avatar?.Dispose();
                        Avatar = new NetAvatarCreator(this, a, lastPlayerUpdate.IsPlayerVR);
                        if ((ConfigManager.SelectedConfigUser?.GetAllowedAvatarComponents(UserId) ??
                             new AllowedAvatarComponent()).Scripting)
                        {
                            foreach (NexboxScript localAvatarScript in Avatar.Avatar.LocalAvatarScripts)
                                Avatar.localAvatarSandboxes.Add(new Sandbox(localAvatarScript, transform,
                                    a.gameObject));
                            foreach (LocalScript ls in Avatar.Avatar.gameObject.GetComponentsInChildren<LocalScript>())
                                Avatar.localAvatarSandboxes.Add(new Sandbox(ls.NexboxScript, transform, ls.gameObject));
                        }
                        if (nameplateTemplate != null)
                            nameplateTemplate.transform.SetLocalPositionAndRotation(
                                new Vector3(0, transform.localScale.y + 0.9f, 0),
                                Quaternion.identity);
                    }));
                }
            }));
        }
        
        private void OnAvatarDownload(string path)
        {
            waitingForAvatarToken = false;
            QuickInvoke.InvokeActionOnMainThread(new Action(() =>
            {
                StartCoroutine(AssetBundleTools.LoadAvatarFromFile(path, a =>
                {
                    if (a == null)
                        return;
                    Avatar?.Dispose();
                    Avatar = new NetAvatarCreator(this, a, lastPlayerUpdate.IsPlayerVR);
                    foreach (NexboxScript localAvatarScript in Avatar.Avatar.LocalAvatarScripts)
                        Avatar.localAvatarSandboxes.Add(new Sandbox(localAvatarScript, transform, a.gameObject));
                    foreach (LocalScript ls in Avatar.Avatar.gameObject.GetComponentsInChildren<LocalScript>())
                        Avatar.localAvatarSandboxes.Add(new Sandbox(ls.NexboxScript, transform, ls.gameObject));
                    if (nameplateTemplate != null)
                        nameplateTemplate.transform.SetLocalPositionAndRotation(
                            new Vector3(0, transform.localScale.y + 0.9f, 0),
                            Quaternion.identity);
                }));
            }));
        }

        private void OnAvatar(CallbackResult<MetaCallback<AvatarMeta>> result)
        {
            if (!result.success)
            {
                APIPlayer.APIObject.GetAvatarMeta(OnAvatar, AvatarId);
                return;
            }

            if (result.result.Meta.Id == AvatarId)
                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                {
                    avatarMeta = result.result.Meta;
                    Builds b = null;
                    foreach (Builds metaBuild in result.result.Meta.Builds)
                        if (metaBuild.BuildPlatform == AssetBundleTools.Platform)
                            b = metaBuild;
                    if (b == null)
                        return;
                    avatarBuild = b;
                    if (avatarMeta.Publicity == AvatarPublicity.OwnerOnly)
                    {
                        if (SocketManager.SharedAvatarTokens.Count(x =>
                                x.avatarId == AvatarId && x.fromUserId == UserId) > 0)
                        {
                            SharedAvatarToken sharedAvatarToken = SocketManager.SharedAvatarTokens.First(x =>
                                x.avatarId == AvatarId && x.fromUserId == UserId);
                            avatarFileToken = sharedAvatarToken;
                        }
                        else
                        {
                            waitingForAvatarToken = true;
                            return;
                        }
                    }

                    string file = $"{APIPlayer.APIObject.Settings.APIURL}file/{result.result.Meta.OwnerId}/{b.FileId}";
                    if (avatarFileToken == null)
                    {
                        //APIPlayer.APIObject.GetFile(OnAvatarDownload, result.result.Meta.OwnerId, b.FileId);
                        APIPlayer.APIObject.GetFileMeta(fmr =>
                        {
                            if (!fmr.success)
                                APIPlayer.APIObject.GetFile(OnAvatarDownload, result.result.Meta.OwnerId, b.FileId);
                            else
                            {
                                DownloadTools.DownloadFile(file, $"{result.result.Meta.Id}.hna",
                                    f => OnAvatarDownload(f), fmr.result.FileMeta.Hash);
                            }
                        }, result.result.Meta.OwnerId, b.FileId);
                    }
                    else
                    {
                        file += $"/{avatarFileToken.avatarToken}";
                        APIPlayer.APIObject.GetFileMeta(fmr =>
                        {
                            if (!fmr.success)
                                APIPlayer.APIObject.GetFile(OnAvatarDownload, result.result.Meta.OwnerId, b.FileId,
                                    avatarFileToken.avatarToken);
                            else
                            {
                                DownloadTools.DownloadFile(file, $"{result.result.Meta.Id}.hna",
                                    f => OnAvatarDownload(f), fmr.result.FileMeta.Hash);
                            }
                        }, result.result.Meta.OwnerId, b.FileId);
                    }
                }));
        }

        private bool waitingForAvatarToken;

        private void Start()
        {
            GameObject g = new GameObject("VoicePosition");
            g.transform.SetParent(transform, false);
            SocketManager.OnAvatarToken += token =>
            {
                if (waitingForAvatarToken && token.fromUserId == UserId && token.avatarId == AvatarId)
                {
                    waitingForAvatarToken = false;
                    avatarFileToken = token;
                    string file = $"{APIPlayer.APIObject.Settings.APIURL}file/{avatarMeta.OwnerId}/{avatarBuild.FileId}";
                    APIPlayer.APIObject.GetFileMeta(fmr =>
                    {
                        if (!fmr.success)
                            APIPlayer.APIObject.GetFile(OnAvatarDownload, avatarMeta.OwnerId, avatarBuild.FileId,
                                avatarFileToken.avatarToken);
                        else
                        {
                            DownloadTools.DownloadFile(file, $"{avatarMeta.Id}.hna",
                                f => OnAvatarDownload(f), fmr.result.FileMeta.Hash);
                        }
                    }, avatarMeta.OwnerId, avatarBuild.FileId);
                }
            };
        }

        private void FixedUpdate() => Avatar?.FixedUpdate();

        private void Update()
        {
            //float interpolationRatio = (float)elapsedFrames / interpolationFramesCount;
            if (instance != null && User == null)
            {
                foreach (User instanceConnectedUser in instance.ConnectedUsers)
                {
                    if (instanceConnectedUser.Id == UserId)
                        User = instanceConnectedUser;
                }
            }
            /*foreach (string key in new List<string>(avatarUpdates.Keys))
                UpdatePlayerObjectUpdate(key);*/
            foreach (WeightedObjectContainer weightedObjectContainer in new List<WeightedObjectContainer>(
                         weightedObjectUpdates))
                weightedObjectContainer.Update();
            //elapsedFrames = (elapsedFrames + 1) % (interpolationFramesCount + 1);
            foreach (SmoothTransform smoothTransform in smoothTransforms.Values)
                smoothTransform.Update();
            foreach (NetHandleCameraLife netHandleCameraLife in handleCameras.Values)
                netHandleCameraLife.SmoothTransform.Update();
            if(lastPlayerUpdate == null)
                Avatar?.Update();
            else
            {
                if (!string.IsNullOrEmpty(lastPlayerUpdate.AvatarId) && (string.IsNullOrEmpty(AvatarId) ||
                                                                         lastPlayerUpdate.AvatarId != AvatarId))
                {
                    AvatarId = lastPlayerUpdate.AvatarId;
                    APIPlayer.APIObject.GetAvatarMeta(OnAvatar, AvatarId);
                    Avatar?.Dispose();
                    Avatar = null;
                }
                if (Avatar != null && Avatar.Avatar.transform.parent == transform)
                {
                    /*foreach (KeyValuePair<string,float> weightedObject in playerUpdate.WeightedObjects)
                        Avatar.HandleNetParameter(weightedObject.Key, weightedObject.Value);*/
                    Avatar.audioSource.volume = lastPlayerUpdate.IsSpeaking ? volume : 0f;
                    if(Avatar != null && Avatar.lipSyncContext != null)
                        Avatar.lipSyncContext.enabled = !LastPlayerTags.Contains("*liptracking");
                }
                if (Avatar != null)
                {
                    if(lastPlayerUpdate.IsPlayerVR != lastVR || (lastVRIKjson != lastPlayerUpdate.VRIKJson && CalibratedAvatarId == AvatarId))
                        Avatar.DestroyIK(lastPlayerUpdate.IsPlayerVR);
                    if (!Avatar.Calibrated && !string.IsNullOrEmpty(lastPlayerUpdate.VRIKJson))
                    {
                        lastCalibrationData =
                            JsonUtility.FromJson<VRIKCalibrator.CalibrationData>(lastPlayerUpdate.VRIKJson);
                        Avatar.CalibrateVRIK(lastPlayerUpdate.IsFBT, lastCalibrationData);
                    }
                    Avatar.Update();
                    Avatar.Update(lastPlayerUpdate.IsFBT);
                    if (Avatar.Calibrated)
                        CalibratedAvatarId = AvatarId;
                }
                lastVR = lastPlayerUpdate.IsPlayerVR;
                lastVRIKjson = lastPlayerUpdate.VRIKJson;
            }
        }

        private void UpdatePlayerUpdate(CoreBone coreBone, NetworkedObject networkedObject)
        {
            if (!smoothTransforms.TryGetValue(coreBone, out SmoothTransform smoothTransform))
            {
                smoothTransform = new SmoothTransform(GetReferenceFromCoreBone(coreBone), false);
                if (smoothTransforms.ContainsKey(coreBone)) smoothTransforms.Remove(coreBone);
                smoothTransforms.Add(coreBone, smoothTransform);
            }
            networkedObject.Apply(smoothTransform);
        }

        private void LateUpdate()
        {
            if (weightedObjectUpdates.Count > 0)
            {
                foreach (WeightedObjectContainer weightedObjectContainer in new List<WeightedObjectContainer>(
                             weightedObjectUpdates))
                    Avatar?.HandleNetParameter(weightedObjectContainer.Weight);
            }
            Avatar?.LateUpdate();
            Avatar?.LateUpdate(GetReferenceFromCoreBone(CoreBone.Head));
        }

        public void Init(string userid, GameInstance gameInstance)
        {
            UserId = userid;
            scene = gameInstance.loadedScene;
            instance = gameInstance;
            APIPlayer.APIObject.GetUser(OnUser, UserId, isUserId: true);
        }

        public void VoiceUpdate(PlayerVoice playerVoice)
        {
            if (Avatar != null && Avatar.Avatar.gameObject.scene == scene)
            {
                //Avatar.opusHandler.DecodeFromVoice(playerVoice);
                IAudioCodec codec = AudioSourceDriver.GetAudioCodecByName(playerVoice.Encoder);
                codec.Decode(playerVoice, Avatar.audioSource);
            }
        }

        private Dictionary<int, NetHandleCameraLife> HandleCameras => new(handleCameras);
        private Dictionary<int, NetHandleCameraLife> handleCameras = new();

        private NetHandleCameraLife GetHandleCamera(int index)
        {
            if (HandleCameras.ContainsKey(index))
                return HandleCameras[index];
            GameObject c = Instantiate(DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("NetHandleCamera").gameObject);
            SceneManager.MoveGameObjectToScene(c, SceneManager.GetActiveScene());
            c.name = "netcamera" + index;
            for (int i = 0; i < c.transform.childCount; i++)
            {
                Transform child = c.transform.GetChild(i);
                child.gameObject.SetActive(true);
            }
            NetHandleCameraLife n = new NetHandleCameraLife(User, c.transform, () => handleCameras.Remove(index));
            handleCameras.Add(index, n);
            return n;
        }

        public void NetworkUpdate(PlayerUpdate playerUpdate) => lastPlayerUpdate = playerUpdate;

        public void NetworkDataUpdate(PlayerDataUpdate playerDataUpdate)
        {
            LastPlayerTags = new List<string>(playerDataUpdate.PlayerAssignedTags);
            LastExtraneousObjects = new Dictionary<string, object>(playerDataUpdate.ExtraneousData);
        }

        private List<WeightedObjectContainer> weightedObjectUpdates = new();

        public void WeightedObject(WeightedObjectUpdate weightedObjectUpdate)
        {
            foreach (WeightedObjectContainer x in new List<WeightedObjectContainer>(weightedObjectUpdates))
            {
                if (x.Equals(weightedObjectUpdate))
                {
                    x.Update(weightedObjectUpdate);
                    return;
                }
            }
            weightedObjectUpdates.Add(new WeightedObjectContainer(weightedObjectUpdate));
        }

        public void ResetWeightedObjects(BulkWeightedObjectUpdate bulkWeightedObjectUpdate)
        {
            weightedObjectUpdates.Clear();
            foreach (WeightedObjectUpdate weightedObjectUpdate in bulkWeightedObjectUpdate.WeightedObjectUpdates)
                WeightedObject(weightedObjectUpdate);
        }

        public void NetworkObjectUpdate(PlayerObjectUpdate playerObjectUpdate)
        {
            foreach (KeyValuePair<int, NetworkedObject> keyValuePair in playerObjectUpdate.Objects)
            {
                if(keyValuePair.Key == (int) CoreBone.Max) continue;
                NetworkedObject networkedObject = keyValuePair.Value;
                if (keyValuePair.Key > (int) CoreBone.Max && User != null)
                {
                    NetHandleCameraLife n = GetHandleCamera(keyValuePair.Key);
                    n.Ping();
                    SmoothTransform c = n.SmoothTransform;
                    c.Position = NetworkConversionTools.float3ToVector3(networkedObject.Position);
                    c.Rotation = Quaternion.Euler(new Vector3(networkedObject.Rotation.x,
                        networkedObject.Rotation.y, networkedObject.Rotation.z));
                    c.Scale = new Vector3(0.01f, 0.01f, 0.01f);
                }
                if (keyValuePair.Key > (int) CoreBone.Max) continue;
                CoreBone coreBone = (CoreBone) keyValuePair.Key;
                if (string.IsNullOrEmpty(networkedObject.ObjectLocation))
                    networkedObject.ObjectLocation = "";
                UpdatePlayerUpdate(coreBone, networkedObject);
            }
        }

        private void OnDestroy()
        {
            foreach (NetHandleCameraLife netHandleCameraLife in HandleCameras.Values)
                netHandleCameraLife.Dispose();
            Avatar?.Dispose();
            foreach (SmoothTransform smoothTransform in smoothTransforms.Values)
                smoothTransform.Dispose();
            smoothTransforms.Clear();
        }

        private class WeightedObjectContainer
        {
            public WeightedObjectUpdate Weight => new()
            {
                Auth = new JoinAuth(),
                PathToWeightContainer = WeightedObjectUpdate.PathToWeightContainer,
                TypeOfWeight = WeightedObjectUpdate.TypeOfWeight,
                Weight = smoothFloat?.Value ?? WeightedObjectUpdate.Weight,
                WeightIndex = WeightedObjectUpdate.WeightIndex
            };

            private SmoothFloat smoothFloat;
            private WeightedObjectUpdate WeightedObjectUpdate;

            public WeightedObjectContainer(WeightedObjectUpdate w)
            {
                smoothFloat = new SmoothFloat(w.Weight, 0.5f);
                Update(w);
            }

            public void Update(WeightedObjectUpdate w)
            {
                WeightedObjectUpdate = w;
                smoothFloat.Value = w.Weight;
            }

            public void Update() => smoothFloat.Update();

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                if (obj.GetType() == typeof(WeightedObjectUpdate))
                {
                    WeightedObjectUpdate w = (WeightedObjectUpdate) obj;
                    return WeightedObjectUpdate.WeightIndex == w.WeightIndex &&
                        WeightedObjectUpdate.TypeOfWeight == w.TypeOfWeight &&
                        WeightedObjectUpdate.PathToWeightContainer ==
                        w.PathToWeightContainer;
                }
                return this == obj;
            }
        }
    }
}