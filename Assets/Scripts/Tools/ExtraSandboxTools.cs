using System.Linq;
using System.Reflection;
using Hypernex.CCK.Unity.Descriptors;
using Hypernex.Game;
using Hypernex.Tools.Text;
using kTools.Mirrors;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Security = Hypernex.CCK.Unity.Internals.Security;

namespace Hypernex.Tools
{
    public static class ExtraSandboxTools
    {
        private static Camera[] GetAllMirrorCameras() => Object.FindObjectsByType<Mirror>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(x =>
            (Camera) typeof(Mirror).GetProperty("reflectionCamera", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(x)).ToArray();
        
        public static void ImplementRestrictions()
        {
            Security.RegisterForceDeleteObject<EventSystem>();
            Security.RegisterForceDeleteObject<StandaloneInputModule>();
            Security.RegisterComponentRestriction<Camera>((component, _) =>
            {
                Camera camera = (Camera) component;
                Camera[] mirrorCameras = GetAllMirrorCameras();
                if(mirrorCameras.Contains(camera)) return;
                camera.gameObject.tag = "Untagged";
                camera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            });
            Security.RegisterComponentRestriction<Mirror>((component, _) =>
            {
                Mirror mirror = (Mirror) component;
                LayerMask mask = LayerMask.GetMask("Default", "Water", "AvatarClip", "ExtraCamera",
                    "LocalPlayer", "MainCamera", "NetAvatar", "UI", "TransparentFX", "Ignore Raycast");
                mirror.layerMask = mask;
                if (mirror.renderers.Count <= 0)
                {
                    // You must use Local mode
                    Object.Destroy(mirror);
                    return;
                }
                mirror.scope = Mirror.OutputScope.Local;
            });
            Security.RegisterComponentRestriction<Canvas>((component, isWorld) =>
            {
                Canvas canvas = (Canvas) component;
                canvas.renderMode = RenderMode.WorldSpace;
                TrackedDeviceGraphicRaycaster trackedDeviceGraphicRaycaster =
                    canvas.gameObject.GetComponent<TrackedDeviceGraphicRaycaster>();
                if (trackedDeviceGraphicRaycaster == null)
                    canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            });
            Security.RegisterComponentRestriction<NetworkSyncDescriptor>((component, _) =>
            {
                NetworkSyncDescriptor networkSyncDescriptor = (NetworkSyncDescriptor) component;
                NetworkSync networkSync = networkSyncDescriptor.gameObject.AddComponent<NetworkSync>();
                networkSync.InstanceHostOnly = networkSyncDescriptor.InstanceHostOnly;
                networkSync.CanSteal = networkSyncDescriptor.CanSteal;
                networkSync.AlwaysSync = networkSyncDescriptor.AlwaysSync;
                if(networkSyncDescriptor.InstanceHostOnly && GameInstance.FocusedInstance.isHost)
                    networkSync.Claim();
            });
            Security.RegisterComponentRestriction<GrabbableDescriptor>((component, _) =>
            {
                GrabbableDescriptor grabbableDescriptor = (GrabbableDescriptor) component;
                Grabbable grabbable = grabbableDescriptor.gameObject.AddComponent<Grabbable>();
                grabbable.ApplyVelocity = grabbableDescriptor.ApplyVelocity;
                grabbable.VelocityAmount = grabbableDescriptor.VelocityAmount;
                grabbable.VelocityThreshold = grabbableDescriptor.VelocityThreshold;
                grabbable.GrabByLaser = grabbableDescriptor.GrabByLaser;
                grabbable.LaserGrabDistance = grabbableDescriptor.LaserGrabDistance;
                grabbable.GrabDistance = grabbableDescriptor.GrabDistance;
                grabbable.GrabDistance = grabbableDescriptor.GrabDistance;
            });
            Security.RegisterComponentRestriction<RespawnableDescriptor>((component, _) =>
            {
                RespawnableDescriptor respawnableDescriptor = (RespawnableDescriptor) component;
                Respawnable respawnable = respawnableDescriptor.gameObject.AddComponent<Respawnable>();
                respawnable.LowestPointRespawnThreshold = respawnableDescriptor.LowestPointRespawnThreshold;
            });
            Security.RegisterComponentRestriction<AudioSource>((component, isWorld) =>
            {
                AudioSource audioSource = (AudioSource) component;
                audioSource.outputAudioMixerGroup = isWorld ? Init.Instance.WorldGroup : Init.Instance.AvatarGroup;
            });
            Security.RegisterComponentRestriction<TextMeshProUGUI>(HandleEmoji);
            Security.RegisterComponentRestriction<TMP_Text>(HandleEmoji);
            Security.RegisterComponentRestriction<TextMeshPro>(HandleEmoji);
        }

        private static void HandleEmoji(Component component, bool isWorld)
        {
            TMP_Text t = component.gameObject.GetComponent<TMP_Text>();
            if(t == null) return;
            if(component.gameObject.GetComponent<TMPEmojiSprite>() != null) return;
#if UNITY_MAC || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            OSTools.ReplaceAllShaders(t.material);
            OSTools.ReplaceAllShaders(t.materialForRendering);
            OSTools.ReplaceAllShaders(t.fontMaterial);
            OSTools.ReplaceAllShaders(t.fontSharedMaterial);
            OSTools.ReplaceAllShaders(t.defaultMaterial);
            t.SetAllDirty();
            t.SetMaterialDirty();
#endif
            component.gameObject.AddComponent<TMPEmojiSprite>();
        }

        internal static Vector3 AddOneUp(this Vector3 pos) => new Vector3(pos.x, pos.y + 1, pos.z);
    }
}