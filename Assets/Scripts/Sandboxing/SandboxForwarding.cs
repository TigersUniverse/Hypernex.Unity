using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CobaltSharp;
using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Sandboxing.SandboxedTypes;
using HypernexSharp.APIObjects;
using Nexbox;
using UnityEngine;
using Bounds = Hypernex.Sandboxing.SandboxedTypes.Bounds;
using Cobalt = Hypernex.Sandboxing.SandboxedTypes.Cobalt;
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
                ["MidpointRounding"] = typeof(MidpointRounding),
                ["SinCos"] = typeof(SinCos),
                ["MathF"] = typeof(ClientMathf),
                ["Item"] = typeof(Item),
                ["ReadonlyItem"] = typeof(ReadonlyItem),
                ["AvatarParameterType"] = typeof(AvatarParameterType),
                ["LocalAvatarLocalAvatar"] = typeof(LocalAvatarLocalAvatar),
                ["Runtime"] = typeof(Runtime),
                ["UI"] = typeof(SandboxedTypes.UI),
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
            ["HumanBodyBones"] = typeof(HumanBodyBones),
            ["AvatarParameterType"] = typeof(AvatarParameterType),
            ["LocalAvatar"] = typeof(LocalLocalAvatar),
            ["NetAvatar"] = typeof(LocalNetAvatar),
            ["ClientNetworkEvent"] = typeof(ClientNetworkEvent),
            ["Runtime"] = typeof(Runtime),
            ["UI"] = typeof(SandboxedTypes.UI),
            ["Color"] = typeof(Color),
            ["ColorBlock"] = typeof(ColorBlock),
            ["WorldProperties"] = typeof(WorldProperties),
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
            ["ScriptEvent"] = typeof(ScriptEvent),
            ["ScriptEvents"] = typeof(ScriptEvents),
            ["Audio"] = typeof(Audio),
            ["Video"] = typeof(Video),
            ["GetMedia"] = typeof(GetMedia),
            ["VideoCodec"] = typeof(VideoCodec),
            ["AudioFormat"] = typeof(AudioFormat),
            ["VideoQuality"] = typeof(VideoQuality),
            ["Cobalt"] = typeof(Cobalt),
            ["CobaltOption"] = typeof(CobaltOption),
            ["CobaltOptions"] = typeof(CobaltOptions),
            ["CobaltDownload"] = typeof(CobaltDownload),
            ["PhysicsBody"] = typeof(PhysicsBody)
        });

        public static Runtime Forward(GameObject attached, IInterpreter interpreter, SandboxRestriction restriction,
            Transform playerRoot, GameInstance gameInstance)
        {
            switch (restriction)
            {
                case SandboxRestriction.LocalAvatar:
                    foreach (KeyValuePair<string,Type> forwardingType in ForwardingLocalAvatarTypes)
                        interpreter.ForwardType(forwardingType.Key, forwardingType.Value);
                    interpreter.CreateGlobal("LocalAvatar", new LocalAvatarLocalAvatar(playerRoot));
                    if(attached.transform == playerRoot)
                        interpreter.CreateGlobal("item", new ReadonlyItem(attached.transform));
                    else
                        interpreter.CreateGlobal("item", new Item(attached.transform));
                    break;
                case SandboxRestriction.Local:
                    // Pre-condition: gameInstance cannot be null
                    foreach (KeyValuePair<string,Type> forwardingType in ForwardingLocalTypes)
                        interpreter.ForwardType(forwardingType.Key, forwardingType.Value);
                    interpreter.CreateGlobal("NetworkEvent", new ClientNetworkEvent(gameInstance));
                    interpreter.CreateGlobal("Players", new LocalNetAvatar(gameInstance));
                    interpreter.CreateGlobal("Events", gameInstance.ScriptEvents);
                    interpreter.CreateGlobal("item", new Item(attached.transform));
                    break;
            }
            interpreter.CreateGlobal("Physics", new Physics(restriction));
            Runtime runtime = new Runtime();
            interpreter.CreateGlobal("Runtime", runtime);
            return runtime;
        }
    }
}