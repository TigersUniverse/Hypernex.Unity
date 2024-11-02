using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Sandboxing.SandboxedTypes.Components;
using Hypernex.Sandboxing.SandboxedTypes.Handlers;
using HypernexSharp.APIObjects;
using Nexbox;
using UnityEngine;
using Avatar = Hypernex.Sandboxing.SandboxedTypes.Avatar;
using Bounds = Hypernex.Sandboxing.SandboxedTypes.Bounds;
using Collider = Hypernex.Sandboxing.SandboxedTypes.Collider;
using Collision = Hypernex.Sandboxing.SandboxedTypes.Collision;
using Color = Hypernex.Sandboxing.SandboxedTypes.Color;
using Physics = Hypernex.Sandboxing.SandboxedTypes.Handlers.Physics;
using Ray = Hypernex.Sandboxing.SandboxedTypes.Ray;
using RaycastHit = Hypernex.Sandboxing.SandboxedTypes.RaycastHit;
using Time = Hypernex.Sandboxing.SandboxedTypes.Time;
using Light = Hypernex.Sandboxing.SandboxedTypes.Components.Light;

namespace Hypernex.Sandboxing
{
    public static class SandboxForwarding
    {
        private static readonly ReadOnlyDictionary<string, Type> BaseForwardingTypes = new(new Dictionary<string, Type>
        {
            ["float2"] = typeof(float2),
            ["float3"] = typeof(float3),
            ["float4"] = typeof(float4),
            ["Item"] = typeof(Item),
            ["PronounObject"] = typeof(PronounObject),
            ["PronounCases"] = typeof(PronounCases),
            ["Pronouns"] = typeof(Pronouns),
            ["HumanBodyBones"] = typeof(HumanBodyBones),
            ["AvatarParameterType"] = typeof(AvatarParameterType),
            ["InstanceContainer"] = typeof(InstanceContainer),
            ["Players"] = typeof(Players),
            ["Player"] = typeof(SandboxedTypes.Player),
            ["Avatar"] = typeof(Avatar),
            ["Runtime"] = typeof(Runtime),
            ["World"] = typeof(World),
            ["WorldProperties"] = typeof(WorldProperties),
            ["ScriptEvents"] = typeof(ScriptEvents),
            ["ScriptEvent"] = typeof(ScriptEvent),
            ["Interactables"] = typeof(Interactables),
            ["Audio"] = typeof(Audio),
            ["Video"] = typeof(Video),
            ["Button"] = typeof(Button),
            ["Dropdown"] = typeof(Dropdown),
            ["Graphic"] = typeof(Graphic),
            ["Scrollbar"] = typeof(Scrollbar),
            ["Slider"] = typeof(Slider),
            ["Text"] = typeof(Text),
            ["TextInput"] = typeof(TextInput),
            ["Toggle"] = typeof(Toggle),
            ["PhysicsBody"] = typeof(PhysicsBody),
            ["Color"] = typeof(Color),
            ["ColorBlock"] = typeof(ColorBlock),
            ["MidpointRounding"] = typeof(MidpointRounding),
            ["SinCos"] = typeof(SinCos),
            ["MathF"] = typeof(ClientMathf),
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
            ["Light"] = typeof(Light),
            ["LightShadows"] = typeof(LightShadows),
            ["LightType"] = typeof(LightType),
        });

        private static readonly ReadOnlyDictionary<string, Type> ForwardingLocalAvatarTypes = new(
            new Dictionary<string, Type>
            {
            }.Union(BaseForwardingTypes).ToDictionary(pair => pair.Key, pair => pair.Value));

        private static readonly ReadOnlyDictionary<string, Type> ForwardingLocalTypes = new(new Dictionary<string, Type>
        {
            ["ClientNetworkEvent"] = typeof(ClientNetworkEvent),
            ["Streaming"] = typeof(Streaming),
            ["StreamDownloadOptions"] = typeof(Streaming.StreamDownloadOptions),
            ["StreamDownload"] = typeof(Streaming.StreamDownload),
            ["PhysicsBody"] = typeof(PhysicsBody)
        }.Union(BaseForwardingTypes).ToDictionary(pair => pair.Key, pair => pair.Value));

        public static InstanceContainer Forward(GameObject attached, IInterpreter interpreter, SandboxRestriction restriction,
            Transform playerRoot, GameInstance gameInstance)
        {
            switch (restriction)
            {
                case SandboxRestriction.LocalAvatar:
                    foreach (KeyValuePair<string,Type> forwardingType in ForwardingLocalAvatarTypes)
                        interpreter.ForwardType(forwardingType.Key, forwardingType.Value);
                    interpreter.CreateGlobal("item", new Item(attached.transform, attached.transform == playerRoot));
                    break;
                case SandboxRestriction.Local:
                    // Pre-condition: gameInstance cannot be null
                    foreach (KeyValuePair<string,Type> forwardingType in ForwardingLocalTypes)
                        interpreter.ForwardType(forwardingType.Key, forwardingType.Value);
                    interpreter.CreateGlobal("item", new Item(attached.transform, false));
                    break;
            }
            InstanceContainer instanceContainer =
                new InstanceContainer(gameInstance, restriction, Avatar.GetPlayerRootFromChild(playerRoot));
            interpreter.CreateGlobal("instance", instanceContainer);
            return instanceContainer;
        }
    }
}