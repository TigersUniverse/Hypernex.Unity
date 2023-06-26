using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Sandboxing.SandboxedTypes;
using Nexbox;
using UnityEngine;
using Bounds = Hypernex.Sandboxing.SandboxedTypes.Bounds;
using Collider = Hypernex.Sandboxing.SandboxedTypes.Collider;
using Collision = Hypernex.Sandboxing.SandboxedTypes.Collision;
using Physics = Hypernex.Sandboxing.SandboxedTypes.Physics;
using Ray = Hypernex.Sandboxing.SandboxedTypes.Ray;
using RaycastHit = Hypernex.Sandboxing.SandboxedTypes.RaycastHit;
using Time = Hypernex.Sandboxing.SandboxedTypes.Time;

namespace Hypernex.Sandboxing
{
    public static class SandboxForwarding
    {
        private static readonly ReadOnlyDictionary<string, Type> ForwardingLocalAvatarTypes = new(
            new Dictionary<string, Type>
            {
                ["HumanBodyBones"] = typeof(HumanBodyBones),
                ["float2"] = typeof(float2),
                ["float3"] = typeof(float3),
                ["float4"] = typeof(float4),
                ["Item"] = typeof(Item),
                ["ReadonlyItem"] = typeof(ReadonlyItem),
                ["LocalAvatar"] = typeof(LocalAvatarLocalAvatar),
                ["Players"] = typeof(LocalAvatarNetAvatar),
                ["Time"] = typeof(Time),
                ["UtcTime"] = typeof(UtcTime),
                ["Bindings"] = typeof(Bindings),
                ["Ray"] = typeof(Ray),
                ["RaycastHit"] = typeof(RaycastHit),
                ["Bounds"] = typeof(Bounds),
                ["Collision"] = typeof(Collision),
                ["Collider"] = typeof(Collider),
                ["Colliders"] = typeof(Colliders),
                ["PenetrationResult"] = typeof(PenetrationResult),
                ["Physics"] = typeof(Physics)
            });

        private static readonly ReadOnlyDictionary<string, Type> ForwardingLocalTypes = new(new Dictionary<string, Type>
        {
            ["float2"] = typeof(float2),
            ["float3"] = typeof(float3),
            ["float4"] = typeof(float4),
            ["Item"] = typeof(Item),
            ["ReadonlyItem"] = typeof(ReadonlyItem),
            ["LocalAvatar"] = typeof(LocalLocalAvatar),
            ["NetAvatar"] = typeof(LocalNetAvatar),
            ["ClientNetworkEvent"] = typeof(ClientNetworkEvent),
            ["Runtime"] = typeof(Runtime),
            ["UI"] = typeof(LocalUI),
            ["World"] = typeof(LocalWorld),
            ["Time"] = typeof(Time),
            ["UtcTime"] = typeof(UtcTime),
            ["Bindings"] = typeof(Bindings),
            ["Ray"] = typeof(Ray),
            ["RaycastHit"] = typeof(RaycastHit),
            ["Bounds"] = typeof(Bounds),
            ["Collision"] = typeof(Collision),
            ["Collider"] = typeof(Collider),
            ["Colliders"] = typeof(Colliders),
            ["PenetrationResult"] = typeof(PenetrationResult),
            ["Physics"] = typeof(Physics),
            ["Interactables"] = typeof(Interactables)
        });

        public static void Forward(IInterpreter interpreter, SandboxRestriction restriction, GameInstance gameInstance)
        {
            switch (restriction)
            {
                case SandboxRestriction.LocalAvatar:
                    foreach (KeyValuePair<string,Type> forwardingType in ForwardingLocalAvatarTypes)
                        interpreter.ForwardType(forwardingType.Key, forwardingType.Value);
                    break;
                case SandboxRestriction.Local:
                    // Pre-condition: gameInstance cannot be null
                    foreach (KeyValuePair<string,Type> forwardingType in ForwardingLocalTypes)
                        interpreter.ForwardType(forwardingType.Key, forwardingType.Value);
                    interpreter.CreateGlobal("NetworkEvent", new ClientNetworkEvent(gameInstance));
                    interpreter.CreateGlobal("Players", new LocalNetAvatar(gameInstance));
                    break;
            }
            interpreter.CreateGlobal("Physics", new Physics(restriction));
        }
    }
}