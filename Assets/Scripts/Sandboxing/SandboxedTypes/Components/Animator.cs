using System;
using System.Collections;
using System.IO;
using Hypernex.Game;
using Hypernex.Tools;
using Nexbox;
using UnityEngine;
using UnityEngine.Networking;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Animator
    {
        private readonly Item item;
        private readonly bool read;
        private UnityEngine.Animator animator;
        
        private static UnityEngine.Animator GetAnimator(Item item)
        {
            UnityEngine.Animator a = item.t.GetComponent<UnityEngine.Animator>();
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

        public Animator(Item i)
        {
            item = i;
            read = i.IsReadOnly;
            animator = GetAnimator(i);
            if (animator == null) throw new Exception("No Animator found on Item at " + i.Path);
        }

        public bool IsValid() => animator != null;

        public bool IsStateName(int layer, string name) => animator.GetCurrentAnimatorStateInfo(layer).IsName(name);
        public bool IsStateTag(int layer, string name) => animator.GetCurrentAnimatorStateInfo(layer).IsTag(name);

        public bool GetBool(string name) => animator.GetBool(name);
        public float GetFloat(string name) => animator.GetFloat(name);
        public int GetInt(string name) => animator.GetInteger(name);

        public void SetTrigger(string name)
        {
            if (read)
                return;
            animator.SetTrigger(name);
        }
        public void SetBool(string name, bool value)
        {
            if (read)
                return;
            animator.SetBool(name, value);
        }
        public void SetFloat(string name, float value)
        {
            if (read)
                return;
            animator.SetFloat(name, value);
        }
        public void SetInt(string name, int value)
        {
            if (read)
                return;
            animator.SetInteger(name, value);
        }
    }
}