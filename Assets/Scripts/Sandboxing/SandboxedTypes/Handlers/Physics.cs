using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes.Handlers
{
    public class Physics
    {
        private GameInstance gameInstance;
        private SandboxRestriction sandboxRestriction;

        public Physics() => throw new Exception("Cannot instantiate Physics!");
        internal Physics(GameInstance gameInstance, SandboxRestriction sandboxRestriction)
        {
            this.gameInstance = gameInstance;
            this.sandboxRestriction = sandboxRestriction;
        }

        private bool GetRead(UnityEngine.Collider collider)
        {
            if (sandboxRestriction == SandboxRestriction.LocalAvatar)
            {
                LocalAvatarCreator localAvatar = LocalPlayer.Instance.avatar;
                Transform localAvatarTransform =
                    localAvatar != null ? localAvatar.Avatar.transform : LocalPlayer.Instance.transform;
                return !AnimationUtility.IsChildOfTransform(collider.transform, localAvatarTransform) &&
                       collider.transform != localAvatarTransform;
            }
            bool found = false;
            List<Transform> allPlayerRoots = GameInstance.GetConnectedUsers(gameInstance, false).Select(c =>
            {
                NetPlayer netPlayer = PlayerManagement.GetNetPlayer(gameInstance, c.Id);
                if (netPlayer == null) return null;
                return netPlayer.transform;
            }).Where(x => x != null).Union(new[] {LocalPlayer.Instance.transform}).ToList();
            foreach (Transform playerRoot in allPlayerRoots)
            {
                if (AnimationUtility.IsChildOfTransform(collider.transform, playerRoot) &&
                    collider.transform != playerRoot)
                    found = true;
            }
            return found;
        }

        public float3 gravity
        {
            get => NetworkConversionTools.Vector3Tofloat3(UnityEngine.Physics.gravity);
            set
            {
                if (sandboxRestriction == SandboxRestriction.LocalAvatar)
                    return;
                UnityEngine.Physics.gravity = NetworkConversionTools.float3ToVector3(value);
            }
        }

        public bool CheckBox(float3 center, float3 halfExtents) => UnityEngine.Physics.CheckBox(
            NetworkConversionTools.float3ToVector3(center), NetworkConversionTools.float3ToVector3(halfExtents));
        public bool CheckBox(float3 center, float3 halfExtents, float4 orientation) => UnityEngine.Physics.CheckBox(
            NetworkConversionTools.float3ToVector3(center), NetworkConversionTools.float3ToVector3(halfExtents),
            NetworkConversionTools.float4ToQuaternion(orientation));

        public bool CheckCapsule(float3 center, float3 halfExtents, float radius) => UnityEngine.Physics.CheckCapsule(
            NetworkConversionTools.float3ToVector3(center), NetworkConversionTools.float3ToVector3(halfExtents),
            radius);

        public bool CheckSphere(float3 center, float radius) =>
            UnityEngine.Physics.CheckSphere(NetworkConversionTools.float3ToVector3(center), radius);

        public float3 ClosestPoint(float3 point, Collider collider, float3 position, float4 rotation) =>
            NetworkConversionTools.Vector3Tofloat3(UnityEngine.Physics.ClosestPoint(
                NetworkConversionTools.float3ToVector3(point), collider.c,
                NetworkConversionTools.float3ToVector3(position), NetworkConversionTools.float4ToQuaternion(rotation)));

        public PenetrationResult ComputePenetration(Collider colliderA, float3 positionA, float4 rotationA, Collider colliderB,
            float3 positionB, float4 rotationB)
        {
            Vector3 direction;
            float distance;
            bool s = UnityEngine.Physics.ComputePenetration(colliderA.c,
                NetworkConversionTools.float3ToVector3(positionA), NetworkConversionTools.float4ToQuaternion(rotationA),
                colliderB.c, NetworkConversionTools.float3ToVector3(positionB),
                NetworkConversionTools.float4ToQuaternion(rotationB), out direction, out distance);
            if (!s)
                return null;
            return new PenetrationResult
            {
                direction = NetworkConversionTools.Vector3Tofloat3(direction),
                distance = distance
            };
        }

        public void SetCollisionIgnore(Collider collider1, Collider collider2, bool ignore)
        {
            if (sandboxRestriction == SandboxRestriction.Local &&
                (AnimationUtility.GetRootOfChild(collider1.c.transform).GetComponent<LocalPlayer>() != null ||
                 AnimationUtility.GetRootOfChild(collider2.c.transform).GetComponent<LocalPlayer>() != null))
                return;
            if (sandboxRestriction == SandboxRestriction.LocalAvatar &&
                (AnimationUtility.GetRootOfChild(collider1.c.transform).GetComponent<LocalPlayer>() == null ||
                 AnimationUtility.GetRootOfChild(collider2.c.transform).GetComponent<LocalPlayer>() == null))
                return;
            UnityEngine.Physics.IgnoreCollision(collider1.c, collider2.c, ignore);
        }

        public bool Linecast(float3 start, float3 end) => UnityEngine.Physics.Linecast(
            NetworkConversionTools.float3ToVector3(start), NetworkConversionTools.float3ToVector3(end));

        public Collider[] OverlapBox(float3 center, float3 halfExtents)
        {
            /*UnityEngine.Collider[] results = Array.Empty<UnityEngine.Collider>();
            UnityEngine.Physics.OverlapBoxNonAlloc(NetworkConversionTools.float3ToVector3(center),
                NetworkConversionTools.float3ToVector3(halfExtents), results);*/
            UnityEngine.Collider[] results = UnityEngine.Physics.OverlapBox(
                NetworkConversionTools.float3ToVector3(center), NetworkConversionTools.float3ToVector3(halfExtents));
            List<Collider> c = new List<Collider>();
            foreach (UnityEngine.Collider collider in results)
                c.Add(new Collider(collider, GetRead(collider)));
            return c.ToArray();
        }
        
        public Collider[] OverlapBox(float3 center, float3 halfExtents, float4 orientation)
        {
            /*UnityEngine.Collider[] results = Array.Empty<UnityEngine.Collider>();
            UnityEngine.Physics.OverlapBoxNonAlloc(NetworkConversionTools.float3ToVector3(center),
                NetworkConversionTools.float3ToVector3(halfExtents), results,
                NetworkConversionTools.float4ToQuaternion(orientation));*/
            UnityEngine.Collider[] results = UnityEngine.Physics.OverlapBox(
                NetworkConversionTools.float3ToVector3(center), NetworkConversionTools.float3ToVector3(halfExtents),
                NetworkConversionTools.float4ToQuaternion(orientation));
            List<Collider> c = new List<Collider>();
            foreach (UnityEngine.Collider collider in results)
                c.Add(new Collider(collider, GetRead(collider)));
            return c.ToArray();
        }
        
        public Collider[] OverlapCapsule(float3 point0, float3 point1, float radius)
        {
            /*UnityEngine.Collider[] results = Array.Empty<UnityEngine.Collider>();
            UnityEngine.Physics.OverlapCapsuleNonAlloc(NetworkConversionTools.float3ToVector3(point0),
                NetworkConversionTools.float3ToVector3(point1), radius, results);*/
            UnityEngine.Collider[] results = UnityEngine.Physics.OverlapCapsule(
                NetworkConversionTools.float3ToVector3(point0), NetworkConversionTools.float3ToVector3(point1), radius);
            List<Collider> c = new List<Collider>();
            foreach (UnityEngine.Collider collider in results)
                c.Add(new Collider(collider, GetRead(collider)));
            return c.ToArray();
        }
        
        public Collider[] OverlapSphere(float3 position, float radius)
        {
            /*UnityEngine.Collider[] results = Array.Empty<UnityEngine.Collider>();
            UnityEngine.Physics.OverlapSphereNonAlloc(NetworkConversionTools.float3ToVector3(position), radius,
                results);*/
            UnityEngine.Collider[] results =
                UnityEngine.Physics.OverlapSphere(NetworkConversionTools.float3ToVector3(position), radius);
            List<Collider> c = new List<Collider>();
            foreach (UnityEngine.Collider collider in results)
                c.Add(new Collider(collider, GetRead(collider)));
            return c.ToArray();
        }
        
        public RaycastHit[] Raycast(Ray ray, float maxDistance = Mathf.Infinity)
        {
            if (UnityEngine.Physics.Raycast(ray.r, out var hit, maxDistance))
            {
                return new RaycastHit[] { new RaycastHit(hit, GetRead(hit.collider)) };
            }
            return new RaycastHit[0];
        }
        
        public RaycastHit[] RaycastAll(Ray ray, float maxDistance = Mathf.Infinity)
        {
            //UnityEngine.RaycastHit[] results = Array.Empty<UnityEngine.RaycastHit>();
            //UnityEngine.Physics.RaycastNonAlloc(ray.r, results, maxDistance);
            UnityEngine.RaycastHit[] results = UnityEngine.Physics.RaycastAll(ray.r, maxDistance);
            List<RaycastHit> c = new List<RaycastHit>();
            foreach (UnityEngine.RaycastHit raycastHit in results)
                c.Add(new RaycastHit(raycastHit, GetRead(raycastHit.collider)));
            return c.ToArray();
        }

        public RaycastHit[] SphereCast(float3 origin, float radius, float3 direction, float maxDistance = Mathf.Infinity)
        {
            if (UnityEngine.Physics.SphereCast(
                NetworkConversionTools.float3ToVector3(origin), radius,
                NetworkConversionTools.float3ToVector3(direction), out var hit, maxDistance))
                {
                    return new RaycastHit[] { new RaycastHit(hit, GetRead(hit.collider)) };
                }
            return new RaycastHit[0];
        }

        public RaycastHit[] SphereCastAll(float3 origin, float radius, float3 direction, float maxDistance = Mathf.Infinity)
        {
            /*UnityEngine.RaycastHit[] results = Array.Empty<UnityEngine.RaycastHit>();
            UnityEngine.Physics.SphereCastNonAlloc(NetworkConversionTools.float3ToVector3(origin), radius,
                NetworkConversionTools.float3ToVector3(direction), results, maxDistance);*/
            UnityEngine.RaycastHit[] results = UnityEngine.Physics.SphereCastAll(
                NetworkConversionTools.float3ToVector3(origin), radius,
                NetworkConversionTools.float3ToVector3(direction), maxDistance);
            List<RaycastHit> c = new List<RaycastHit>();
            foreach (UnityEngine.RaycastHit raycastHit in results)
                c.Add(new RaycastHit(raycastHit, GetRead(raycastHit.collider)));
            return c.ToArray();
        }
    }
}