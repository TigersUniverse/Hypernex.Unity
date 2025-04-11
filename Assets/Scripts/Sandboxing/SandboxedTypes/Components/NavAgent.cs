using System;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using UnityEngine;
using UnityEngine.AI;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class NavAgent
    {
        private readonly Item item;
        private readonly bool read;
        private NavMeshAgent agent;
        
        private static NavMeshAgent GetAgent(Item item)
        {
            NavMeshAgent a = item.t.GetComponent<NavMeshAgent>();
            if (a == null)
                return null;
            return a;
        }
        
        public bool Enabled
        {
            get => agent == null ? false : agent.enabled;
            set
            {
                if (read || agent == null) return;
                agent.enabled = value;
            }
        }

        public NavAgent(Item i)
        {
            item = i;
            read = i.IsReadOnly;
            agent = GetAgent(i);
            if (agent == null) throw new Exception("No NavMeshAgent found on Item at " + i.Path);
        }

        public float Speed
        {
            get => agent == null ? default : agent.speed;
            set
            {
                if (read || agent == null) return;
                agent.speed = value;
            }
        }

        public float3 Destination
        {
            get => agent == null ? default : NetworkConversionTools.Vector3Tofloat3(agent.destination);
            set
            {
                if (read || agent == null) return;
                agent.destination = NetworkConversionTools.float3ToVector3(value);
            }
        }

        public bool IsPathPending
        {
            get => agent == null ? false : agent.pathPending;
        }

        public NavPath Path
        {
            get => agent == null ? null : new NavPath(agent.path);
            set
            {
                if (read || agent == null || value == null) return;
                agent.path = value.r;
            }
        }

        public bool Warp(float3 position)
        {
            if (read || agent == null) return false;
            return agent.Warp(NetworkConversionTools.float3ToVector3(position));
        }
    }
}