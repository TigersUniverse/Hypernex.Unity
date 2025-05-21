using System;
using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using UnityEngine.AI;

namespace Hypernex.Sandboxing.SandboxedTypes.Handlers
{
    public class NavMesh
    {
        private GameInstance gameInstance;

        public NavMesh()
        {
            throw new Exception("Cannot instantiate NavMesh!");
        }

        internal NavMesh(GameInstance gameInstance) => this.gameInstance = gameInstance;

        public int AllAreas => UnityEngine.AI.NavMesh.AllAreas;

        public NavMeshHit Raycast(float3 source, float3 target, int areaMask)
        {
            UnityEngine.AI.NavMeshHit n;
            bool b = UnityEngine.AI.NavMesh.Raycast(NetworkConversionTools.float3ToVector3(source),
                NetworkConversionTools.float3ToVector3(target), out n, areaMask);
            if (!b) return null;
            return new NavMeshHit(n);
        }

        public bool CalculatePath(float3 source, float3 target, int areaMask, NavPath path)
        {
            return UnityEngine.AI.NavMesh.CalculatePath(NetworkConversionTools.float3ToVector3(source), NetworkConversionTools.float3ToVector3(target), areaMask, path.r);
        }

        public NavMeshHit FindClosestEdge(float3 source, int areaMask)
        {
            UnityEngine.AI.NavMeshHit h;
            bool b = UnityEngine.AI.NavMesh.FindClosestEdge(NetworkConversionTools.float3ToVector3(source), out h,
                areaMask);
            if (!b) return null;
            return new NavMeshHit(h);
        }

        public NavMeshHit SamplePosition(float3 source, float maxDistance, int areaMask)
        {
            UnityEngine.AI.NavMeshHit h;
            bool b = UnityEngine.AI.NavMesh.SamplePosition(NetworkConversionTools.float3ToVector3(source), out h,
                maxDistance, areaMask);
            if (!b) return null;
            return new NavMeshHit(h);
        }
    }
}