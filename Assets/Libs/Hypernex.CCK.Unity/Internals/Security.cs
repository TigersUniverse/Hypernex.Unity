using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hypernex.CCK.Unity.Assets;
using Hypernex.CCK.Unity.Descriptors;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using Avatar = Hypernex.CCK.Unity.Assets.Avatar;
using Object = UnityEngine.Object;

namespace Hypernex.CCK.Unity.Internals
{
    public static class Security
    {
        public static Component[] GetOffendingComponents(Avatar avatar, AllowedAvatarComponent allowedAvatarComponent,
            Type[] additionalTypes = null) => GetOffendingComponents(
            avatar.transform.GetComponentsInChildren<Transform>(true).Concat(new[] {avatar.transform}).ToArray(),
            GetAllowedAvatarComponents(allowedAvatarComponent, additionalTypes));

        public static Component[] GetOffendingComponents(Scene scene, Type[] additionalTypes = null) =>
            GetOffendingComponents(
                Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(x => x.gameObject.scene == scene).ToArray(), GetAllowedWorldComponents(additionalTypes));
        
        private static Component[] GetOffendingComponents(Transform[] transforms, Type[] allowedTypes)
        {
            List<Component> deniedComponents = new List<Component>();
            foreach (Transform transform in transforms)
            {
                List<Component> allowedComponents = new List<Component>();
                List<Component> allComponents = new List<Component>();
                transform.gameObject.GetComponents(allComponents);
                foreach (Type allowedType in allowedTypes)
                {
                    List<Component> currentAllowedComponents = new List<Component>();
                    transform.gameObject.GetComponents(allowedType, currentAllowedComponents);
                    allowedComponents = allowedComponents.Concat(currentAllowedComponents).ToList();
                }
                deniedComponents.AddRange(allComponents.Where(component =>
                    !allowedComponents.Contains(component) && !deniedComponents.Contains(component)));
            }
            return deniedComponents.ToArray();
        }

        public static void RemoveOffendingItems(Avatar avatar,
            AllowedAvatarComponent allowedAvatarComponent, Type[] additionalTypes = null) =>
            RemoveOffendingItems(
                avatar.transform.GetComponentsInChildren<Transform>().Concat(new[] {avatar.transform}).ToArray(),
                GetAllowedAvatarComponents(allowedAvatarComponent, additionalTypes));

        public static void RemoveOffendingItems(Scene scene, Type[] additionalTypes = null) => RemoveOffendingItems(
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(x => x.gameObject.scene == scene).ToArray(), GetAllowedWorldComponents(additionalTypes));

        private static void RemoveOffendingItems(Transform[] transformsToCheck, Type[] allowedTypes,
            bool destroyImmediate = false) => GetOffendingComponents(transformsToCheck, allowedTypes).ToList().ForEach(
            x =>
            {
                if (x == null || x.gameObject == null) return;
                // Removes the whole GameObject if told to
                bool forceRemoveObject = ForceDeleteGameObject.Contains(x.GetType());
                if (forceRemoveObject)
                {
                    if (destroyImmediate)
                        Object.DestroyImmediate(x.gameObject);
                    else
                        Object.Destroy(x.gameObject);
                    return;
                }
                // Tries to remove the components, and if it can't, it removes the whole GameObject
                if (destroyImmediate)
                {
                    try
                    {
                        Object.DestroyImmediate(x);
                    }
                    catch (Exception)
                    {
                        Object.DestroyImmediate(x.gameObject);
                    }
                    return;
                }
                try
                {
                    Object.Destroy(x);
                }
                catch (Exception)
                {
                    Object.Destroy(x.gameObject);
                }
            });
        
        private static Type[] GetAllowedAvatarComponents(AllowedAvatarComponent allowedAvatarComponent, Type[] additionalTypes = null)
        {
            Type[] allowedAvatar = AvatarComponents.Union(BuiltInMinimum).ToArray();
            if (allowedAvatarComponent.Scripting)
                allowedAvatar = allowedAvatar.Union(ScriptingTypes.ToArray()).ToArray();
            if (allowedAvatarComponent.Physics)
                allowedAvatar = allowedAvatar.Union(PhysicsTypes.ToArray()).ToArray();
            if (allowedAvatarComponent.Audio)
                allowedAvatar = allowedAvatar.Union(AudioTypes.ToArray()).ToArray();
            if (allowedAvatarComponent.UI)
                allowedAvatar = allowedAvatar.Union(UITypes.ToArray()).ToArray();
            if (allowedAvatarComponent.Light)
                allowedAvatar = allowedAvatar.Union(LightTypes.ToArray()).ToArray();
            if (allowedAvatarComponent.Particle)
                allowedAvatar = allowedAvatar.Union(ParticleTypes.ToArray()).ToArray();
            if (additionalTypes != null) return allowedAvatar.Concat(additionalTypes).ToArray();
            return allowedAvatar;
        }

        private static Type[] GetAllowedWorldComponents(Type[] additionalTypes = null)
        {
            Type[] allowedWorld = WorldComponents.Union(BuiltInMinimum).Union(ScriptingTypes.ToArray())
                .Union(PhysicsTypes.ToArray()).Union(AudioTypes.ToArray()).Union(UITypes.ToArray())
                .Union(LightTypes.ToArray()).Union(ParticleTypes.ToArray()).ToArray();
            if (additionalTypes != null) return allowedWorld.Concat(additionalTypes).ToArray();
            return allowedWorld;
        }

        private static readonly Type[] AvatarComponents =
        {
            typeof(Avatar),
            typeof(FaceTrackingDescriptor)
        };

        private static readonly Type[] WorldComponents =
        {
            // Hypernex.Unity Components
            typeof(World),
            typeof(GrabbableDescriptor),
            typeof(NetworkSyncDescriptor),
            typeof(RespawnableDescriptor),
            typeof(VideoPlayerDescriptor),
            // Unity Components
            typeof(ReflectionProbe),
            typeof(Camera),
            typeof(FlareLayer)
        };

        private static readonly Type[] BuiltInMinimum = {
            // Hypernex.Unity Components
            typeof(AssetIdentifier),
            // Unity Components
            typeof(AimConstraint),
            typeof(Animation),
            typeof(Animator),
            typeof(LookAtConstraint),
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(ParentConstraint),
            typeof(PositionConstraint),
            typeof(RotationConstraint),
            typeof(ScaleConstraint),
            typeof(SkinnedMeshRenderer),
            typeof(Transform)
        };

        public static readonly SecurityList<Type> ScriptingTypes = new SecurityList<Type>(new List<Type>
        {
            typeof(LocalScript)
        });
        
        public static readonly SecurityList<Type> PhysicsTypes = new SecurityList<Type>(new List<Type>
        {
            typeof(Rigidbody),
            typeof(Cloth),
            typeof(Collider),
            typeof(CharacterJoint),
            typeof(ConfigurableJoint),
            typeof(FixedJoint),
            typeof(HingeJoint),
            typeof(SpringJoint)
        });

        public static readonly SecurityList<Type> AudioTypes = new SecurityList<Type>(new List<Type>
        {
            typeof(AudioSource)
        });

        public static readonly SecurityList<Type> UITypes = new SecurityList<Type>(new List<Type>
        {
            typeof(Canvas),
            typeof(CanvasRenderer)
        });
        
        public static readonly SecurityList<Type> LightTypes = new SecurityList<Type>(new List<Type>
        {
            typeof(Light),
            typeof(LightProbeGroup)
        });
        
        public static readonly SecurityList<Type> ParticleTypes = new SecurityList<Type>(new List<Type>
        {
            typeof(ParticleSystem),
            typeof(ParticleSystemRenderer),
            typeof(LineRenderer),
            typeof(TrailRenderer)
        });

        private static List<Type> ForceDeleteGameObject = new List<Type>();

        public static void RegisterForceDeleteObject<T>() => ForceDeleteGameObject.Add(typeof(T));

        private static readonly Dictionary<Type, Action<Component, bool>> ComponentRestrictions =
            new Dictionary<Type, Action<Component, bool>>();

        public static void RegisterComponentRestriction<T>(Action<Component, bool> a)
        {
            if (ComponentRestrictions.ContainsKey(typeof(T))) ComponentRestrictions.Remove(typeof(T));
            ComponentRestrictions.Add(typeof(T), a);
        }

        public static void ApplyComponentRestrictions(Avatar avatar) => ApplyComponentRestrictions(
            avatar.transform.GetComponentsInChildren<Transform>(true).Concat(new[] {avatar.transform})
                .Select(x => x.gameObject), false);

        public static void ApplyComponentRestrictions(Scene scene) => ApplyComponentRestrictions(
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(x => x.gameObject.scene == scene).Select(x => x.gameObject), true);

        private static void ApplyComponentRestrictions(IEnumerable<GameObject> gameObjects, bool isWorld)
        {
            List<Component> appliedComponents = new List<Component>();
            foreach (GameObject gameObject in gameObjects)
            {
                if(gameObject == null) continue;
                List<Component> currentComponents = new List<Component>();
                gameObject.GetComponents(currentComponents);
                foreach (Component component in currentComponents)
                {
                    if(component == null || appliedComponents.Contains(component)) continue;
                    Type componentType = component.GetType();
                    if (!ComponentRestrictions.TryGetValue(componentType, out var typeAction)) continue;
                    typeAction.Invoke(component, isWorld);
                    appliedComponents.Add(component);
                }
            }
        }

        public static Type[] GetDynamicBoneTypes(Assembly assembly)
        {
            List<Type> types = new List<Type>();
            Type dynamicBone = assembly.GetType("DynamicBone");
            Type dynamicBoneCollider = assembly.GetType("DynamicBoneCollider");
            Type dynamicBonePlaneCollider = assembly.GetType("DynamicBonePlaneCollider");
            Type dynamicBoneColliderBase = assembly.GetType("DynamicBoneColliderBase");
            if(dynamicBone != null) types.Add(dynamicBone);
            if(dynamicBoneCollider != null) types.Add(dynamicBoneCollider);
            if(dynamicBonePlaneCollider != null) types.Add(dynamicBonePlaneCollider);
            if(dynamicBoneColliderBase != null) types.Add(dynamicBoneColliderBase);
            return types.ToArray();
        }
    }

    public struct AllowedAvatarComponent
    {
        public bool Scripting;
        public bool Physics;
        public bool Audio;
        public bool UI;
        public bool Light;
        public bool Particle;

        public AllowedAvatarComponent(bool scripting, bool physics, bool audio, bool ui, bool light, bool particle)
        {
            Scripting = scripting;
            Physics = physics;
            Audio = audio;
            UI = ui;
            Light = light;
            Particle = particle;
        }
    }
}