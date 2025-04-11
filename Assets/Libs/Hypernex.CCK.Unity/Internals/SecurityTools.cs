using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hypernex.CCK.Unity.Internals;
using kTools.Mirrors;
#if STEAMAUDIO_ENABLED
using SteamAudio;
#endif
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Security = Hypernex.CCK.Unity.Internals.Security;
#if MAGICACLOTH2
using MagicaCloth2;
#endif

namespace Hypernex.CCK.Unity.Internals
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
#if STEAMAUDIO_ENABLED
            AdditionalAllowedWorldTypes.Allow(typeof(SteamAudioSource));
            AdditionalAllowedWorldTypes.Allow(typeof(SteamAudioGeometry));
            AdditionalAllowedWorldTypes.Allow(typeof(SteamAudioDynamicObject));
            AdditionalAllowedWorldTypes.Allow(typeof(SteamAudioProbeBatch));
            AdditionalAllowedWorldTypes.Allow(typeof(SteamAudioBakedSource));
            AdditionalAllowedWorldTypes.Allow(typeof(SteamAudioBakedListener));
            AdditionalAllowedWorldTypes.Allow(typeof(SteamAudioAmbisonicSource));
            AdditionalAllowedWorldTypes.Allow(typeof(SteamAudioStaticMesh));
#endif
            try
            {
                // This can fail easily if the assembly is tampered with
                Security.GetDynamicBoneTypes(Assembly.GetExecutingAssembly()).ToList().ForEach(Security.PhysicsTypes.Allow);
            } catch(Exception){}
#if URP
            Security.LightTypes.Allow(typeof(UniversalAdditionalLightData));
            AdditionalAllowedWorldTypes.Allow(typeof(UniversalAdditionalCameraData));
            AdditionalAllowedWorldTypes.Allow(typeof(Volume));
            AdditionalAllowedWorldTypes.Allow(typeof(Mirror));
#endif
#if UI
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
                typeof(GraphicRaycaster),
                typeof(HorizontalLayoutGroup),
                typeof(VerticalLayoutGroup),
                typeof(GridLayoutGroup),
                typeof(ScrollRect),
                typeof(Scrollbar),
                typeof(Selectable),
                typeof(Outline),
                typeof(PositionAsUV1),
                typeof(Shadow),
                typeof(Mask)
            };
            uiTypes.ForEach(Security.UITypes.Allow);
#endif
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
    }
}