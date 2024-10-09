using System;
using Hypernex.Game;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class WorldProperties
    {
        private const string WRITE_ERROR = "Cannot write when in readonly mode!";
        
        private GameInstance gameInstance;
        private bool read;
        
        public bool AllowRespawn
        {
            get => gameInstance.World.AllowRespawn;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                gameInstance.World.AllowRespawn = value;
            }
        }
        public float Gravity
        {
            get => gameInstance.World.Gravity;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                gameInstance.World.Gravity = value;
            }
        }
        public float JumpHeight
        {
            get => gameInstance.World.JumpHeight;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                gameInstance.World.JumpHeight = value;
            }
        }
        public float WalkSpeed
        {
            get => gameInstance.World.WalkSpeed;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                gameInstance.World.WalkSpeed = value;
            }
        }
        public float RunSpeed
        {
            get => gameInstance.World.RunSpeed;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                gameInstance.World.RunSpeed = value;
            }
        }
        public bool AllowRunning
        {
            get => gameInstance.World.AllowRunning;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                gameInstance.World.AllowRunning = value;
            }
        }
        public bool AllowScaling
        {
            get => gameInstance.World.AllowScaling;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                gameInstance.World.AllowScaling = value;
            }
        }
        public bool LockAvatarSwitching
        {
            get => gameInstance.World.LockAvatarSwitching;
            set
            {
                if (read) throw new Exception(WRITE_ERROR);
                gameInstance.World.LockAvatarSwitching = value;
            }
        }
        
        public WorldProperties(){ throw new Exception("Cannot instantiate WorldProperties!"); }
        
        internal WorldProperties(GameInstance gameInstance, bool read)
        {
            this.gameInstance = gameInstance;
            this.read = read;
        }
    }
}