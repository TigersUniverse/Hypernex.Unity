using Hypernex.CCK.Unity;
using Hypernex.Game;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class WorldProperties
    {
        public bool AllowRespawn = true;
        public float Gravity = -9.87f;
        public float JumpHeight = 1.0f;
        public float WalkSpeed = 5f;
        public float RunSpeed = 10f;
        public bool AllowRunning = true;
        public bool AllowScaling = true;
        public bool LockAvatarSwitching;

        public WorldProperties()
        {
            if (GameInstance.FocusedInstance == null || GameInstance.FocusedInstance.World == null)
                return;
            World world = GameInstance.FocusedInstance.World;
            AllowRespawn = world.AllowRespawn;
            Gravity = world.Gravity;
            JumpHeight = world.JumpHeight;
            WalkSpeed = world.WalkSpeed;
            RunSpeed = world.RunSpeed;
            AllowRunning = world.AllowRunning;
            AllowScaling = world.AllowScaling;
            LockAvatarSwitching = world.LockAvatarSwitching;
        }
        
        internal WorldProperties(World world)
        {
            AllowRespawn = world.AllowRespawn;
            Gravity = world.Gravity;
            JumpHeight = world.JumpHeight;
            WalkSpeed = world.WalkSpeed;
            RunSpeed = world.RunSpeed;
            AllowRunning = world.AllowRunning;
            AllowScaling = world.AllowScaling;
            LockAvatarSwitching = world.LockAvatarSwitching;
        }
    }
}