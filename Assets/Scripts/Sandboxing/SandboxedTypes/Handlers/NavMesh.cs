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

        public bool Raycast(float3 source, float3 target, NavMeshHit meshHit, int areaMask)
        {
            return UnityEngine.AI.NavMesh.Raycast(NetworkConversionTools.float3ToVector3(source), NetworkConversionTools.float3ToVector3(target), out meshHit.r, areaMask);
        }

        public bool CalculatePath(float3 source, float3 target, int areaMask, NavPath path)
        {
            return UnityEngine.AI.NavMesh.CalculatePath(NetworkConversionTools.float3ToVector3(source), NetworkConversionTools.float3ToVector3(target), areaMask, path.r);
        }

        public bool FindClosestEdge(float3 source, NavMeshHit meshHit, int areaMask)
        {
            return UnityEngine.AI.NavMesh.FindClosestEdge(NetworkConversionTools.float3ToVector3(source), out meshHit.r, areaMask);
        }

        public bool SamplePosition(float3 source, NavMeshHit meshHit, float maxDistance, int areaMask)
        {
            return UnityEngine.AI.NavMesh.SamplePosition(NetworkConversionTools.float3ToVector3(source), out meshHit.r, maxDistance, areaMask);
        }
    }
}