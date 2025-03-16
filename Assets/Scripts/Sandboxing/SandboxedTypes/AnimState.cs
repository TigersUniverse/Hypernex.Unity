using System;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class AnimState
    {
        private readonly UnityEngine.Animation item;
        private readonly AnimationState state;
        private readonly bool read;
        private readonly string name;

        public AnimState() => throw new Exception("AnimState cannot be created by a Script!");
        internal AnimState(UnityEngine.Animation anim, bool read, string name)
        {
            item = anim;
            this.read = read;
            this.name = name;
            state = anim[name];
        }

        public string Name => name;
        public float Speed
        {
            get => state.speed;
            set
            {
                if (read)
                    return;
                state.speed = value;
            }
        }
        public float Time
        {
            get => state.time;
            set
            {
                if (read)
                    return;
                state.time = value;
            }
        }
        public float Length
        {
            get => state.length;
        }
    }
}