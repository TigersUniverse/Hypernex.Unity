using System;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Animation
    {
        private readonly Item item;
        private readonly bool read;
        private UnityEngine.Animation animator;
        
        private static UnityEngine.Animation GetAnimator(Item item)
        {
            UnityEngine.Animation a = item.t.GetComponent<UnityEngine.Animation>();
            if (a == null)
                return null;
            return a;
        }

        public bool Enabled
        {
            get => animator == null ? false : animator.enabled;
            set
            {
                if (read || animator == null) return;
                animator.enabled = value;
            }
        }

        public Animation(Item i)
        {
            item = i;
            read = i.IsReadOnly;
            animator = GetAnimator(i);
            if (animator == null) throw new Exception("No Animation found on Item at " + i.Path);
        }

        public bool IsValid() => animator != null;

        public void Play(string name)
        {
            if (read)
                return;
            animator.Play(name);
        }
        public void Stop(string name)
        {
            if (read)
                return;
            animator.Stop(name);
        }
        public AnimState GetState(string name)
        {
            return new AnimState(animator, read, name);
        }
    }
}