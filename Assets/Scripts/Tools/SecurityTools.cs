using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hypernex.CCK.Unity;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Game;
using kTools.Mirrors;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using Security = Hypernex.CCK.Unity.Security;
#if MAGICACLOTH2
using MagicaCloth2;
#endif

namespace Hypernex.Tools
{
    public static class SecurityTools
    {
        public static SecurityList<Type> AdditionalAllowedAvatarTypes = new();
        public static SecurityList<Type> AdditionalAllowedWorldTypes = new();
        
        public static void AllowExtraTypes()
        {
#if MAGICACLOTH2
            List<Type> mTypes = new List<Type>
            {
                typeof(MagicaCloth),
                typeof(MagicaCapsuleCollider),
                typeof(MagicaPlaneCollider),
                typeof(MagicaSphereCollider),
                typeof(MagicaWindZone)
            };
            mTypes.ForEach(Security.PhysicsTypes.Allow);
#endif
            try
            {
                // This can fail easily if the assembly is tampered with
                Security.GetDynamicBoneTypes(Assembly.GetExecutingAssembly()).ToList().ForEach(Security.PhysicsTypes.Allow);
            } catch(Exception){}
            Security.LightTypes.Allow(typeof(UniversalAdditionalLightData));
            AdditionalAllowedWorldTypes.Allow(typeof(UniversalAdditionalCameraData));
            AdditionalAllowedWorldTypes.Allow(typeof(Mirror));
            List<Type> uiTypes = new List<Type>
            {
                typeof(Button),
                typeof(Slider),
                typeof(Toggle),
                typeof(Image),
                typeof(RawImage),
                typeof(LayoutElement),
                typeof(RectMask2D),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            };
            uiTypes.ForEach(Security.UITypes.Allow);
            List<Type> tmpTypes = new List<Type>
            {
                typeof(TMP_Text),
                typeof(TMP_Dropdown),
                typeof(TMP_InputField),
                typeof(TextMeshPro),
                typeof(TextMeshProUGUI)
            };
            tmpTypes.ForEach(Security.UITypes.Allow);
            Security.PhysicsTypes.Allow(typeof(AkBoneDynamics.AkBoneDynamics));
            Security.LightTypes.Allow(typeof(AkBoneDynamics.AkBoneDynamicsLight));
            Security.PhysicsTypes.Allow(typeof(AkBoneDynamics.AkBDCollider));
            Security.PhysicsTypes.Allow(typeof(AkBoneDynamics.AkBDCapsuleCollider));
            Security.PhysicsTypes.Allow(typeof(AkBoneDynamics.AkBDSphereCollider));
            Security.PhysicsTypes.Allow(typeof(AkBoneDynamics.AkBDPlaneCollider));
        }

        private static Camera[] GetAllMirrorCameras() => Object.FindObjectsOfType<Mirror>(true).Select(x =>
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
                if (!isWorld) canvas.renderMode = RenderMode.WorldSpace;
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    TrackedDeviceGraphicRaycaster trackedDeviceGraphicRaycaster =
                        canvas.gameObject.GetComponent<TrackedDeviceGraphicRaycaster>();
                    if (trackedDeviceGraphicRaycaster == null)
                        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                }
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
                audioSource.spatialize = true;
            });
        }
    }
}