using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Sandboxing.SandboxedTypes;
using HypernexSharp.APIObjects;
using Nexbox;
using UnityEngine;
using Bounds = Hypernex.Sandboxing.SandboxedTypes.Bounds;
using Collider = Hypernex.Sandboxing.SandboxedTypes.Collider;
using Collision = Hypernex.Sandboxing.SandboxedTypes.Collision;
using Color = Hypernex.Sandboxing.SandboxedTypes.Color;
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
                ["AvatarParameterType"] = typeof(AvatarParameterType),
                ["LocalAvatarLocalAvatar"] = typeof(LocalAvatarLocalAvatar),
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
                ["Physics"] = typeof(Physics),
                ["PronounObject"] = typeof(PronounObject),
                ["PronounCases"] = typeof(PronounCases),
                ["Pronouns"] = typeof(Pronouns)
            });

        private static readonly ReadOnlyDictionary<string, Type> ForwardingLocalTypes = new(new Dictionary<string, Type>
        {
            ["float2"] = typeof(float2),
            ["float3"] = typeof(float3),
            ["float4"] = typeof(float4),
            ["Item"] = typeof(Item),
            ["ReadonlyItem"] = typeof(ReadonlyItem),
            ["AvatarParameterType"] = typeof(AvatarParameterType),
            ["LocalAvatar"] = typeof(LocalLocalAvatar),
            ["NetAvatar"] = typeof(LocalNetAvatar),
            ["ClientNetworkEvent"] = typeof(ClientNetworkEvent),
            ["Runtime"] = typeof(Runtime),
            ["UI"] = typeof(LocalUI),
            ["Color"] = typeof(Color),
            ["ColorBlock"] = typeof(ColorBlock),
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
            ["Interactables"] = typeof(Interactables),
            ["PronounObject"] = typeof(PronounObject),
            ["PronounCases"] = typeof(PronounCases),
            ["Pronouns"] = typeof(Pronouns),
            ["ScriptEvents"] = typeof(ScriptEvents)
        });

        public static void Forward(IInterpreter interpreter, SandboxRestriction restriction, Transform playerRoot, 
            GameInstance gameInstance)
        {
            switch (restriction)
            {
                case SandboxRestriction.LocalAvatar:
                    foreach (KeyValuePair<string,Type> forwardingType in ForwardingLocalAvatarTypes)
                        interpreter.ForwardType(forwardingType.Key, forwardingType.Value);
                    interpreter.CreateGlobal("LocalAvatar", new LocalAvatarLocalAvatar(playerRoot));
                    break;
                case SandboxRestriction.Local:
                    // Pre-condition: gameInstance cannot be null
                    foreach (KeyValuePair<string,Type> forwardingType in ForwardingLocalTypes)
                        interpreter.ForwardType(forwardingType.Key, forwardingType.Value);
                    interpreter.CreateGlobal("NetworkEvent", new ClientNetworkEvent(gameInstance));
                    interpreter.CreateGlobal("Players", new LocalNetAvatar(gameInstance));
                    interpreter.CreateGlobal("Events", gameInstance.ScriptEvents);
                    break;
            }
            interpreter.CreateGlobal("Physics", new Physics(restriction));
        }
    }
}