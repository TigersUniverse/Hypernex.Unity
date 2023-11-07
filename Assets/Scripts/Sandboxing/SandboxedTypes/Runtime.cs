using System;
using System.Collections;
using System.Collections.Generic;
using Hypernex.Tools;
using Nexbox;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Runtime : IDisposable
    {
        internal List<SandboxFunc> OnFixedUpdates => new (onFixedUpdates);
        private List<SandboxFunc> onFixedUpdates = new ();
        
        internal List<SandboxFunc> OnUpdates => new (onUpdates);
        private List<SandboxFunc> onUpdates = new ();

        internal List<SandboxFunc> OnLateUpdates => new(onLateUpdates);
        private List<SandboxFunc> onLateUpdates = new();

        private Dictionary<SandboxFunc, CoroutineHolder> repeats = new();
        
        public void OnFixedUpdate(SandboxFunc s) => onFixedUpdates.Add(s);
        public void RemoveOnFixedUpdate(SandboxFunc s) => onFixedUpdates.Add(s);
        public void OnUpdate(SandboxFunc s) => onUpdates.Add(s);
        public void RemoveOnUpdate(SandboxFunc s) => onUpdates.Remove(s);
        public void OnLateUpdate(SandboxFunc s) => onLateUpdates.Add(s);
        public void RemoveOnLateUpdate(SandboxFunc s) => onLateUpdates.Remove(s);

        public void RepeatSeconds(SandboxFunc s, float waitTime)
        {
            CoroutineHolder c = new CoroutineHolder();
            repeats.Add(s, c);
            c.Start(s, waitTime);
        }

        public void RemoveRepeatSeconds(SandboxFunc s)
        {
            if (repeats.ContainsKey(s))
            {
                repeats[s].Dispose();
                repeats.Remove(s);
            }
        }

        public void RunAfterSeconds(SandboxFunc s, float time) => CoroutineRunner.Instance.StartCoroutine(_w(s, time));

        public void FixedUpdate() => OnFixedUpdates.ForEach(x =>
        {
            try
            {
                SandboxFuncTools.InvokeSandboxFunc(x);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error(e);
            }
        });

        public void Update() => OnUpdates.ForEach(x =>
        {
            try
            {
                SandboxFuncTools.InvokeSandboxFunc(x);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error(e);
            }
        });
        
        public void LateUpdate() => OnLateUpdates.ForEach(x =>
        {
            try
            {
                SandboxFuncTools.InvokeSandboxFunc(x);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Error(e);
            }
        });

        private IEnumerator _w(SandboxFunc s, float t)
        {
            yield return new WaitForSeconds(t);
            SandboxFuncTools.InvokeSandboxFunc(s);
        }
        
        public void Dispose()
        {
            onFixedUpdates.Clear();
            onUpdates.Clear();
            onLateUpdates.Clear();
            foreach (SandboxFunc sandboxFunc in new List<SandboxFunc>(repeats.Keys))
                RemoveRepeatSeconds(sandboxFunc);
        }

        private class CoroutineHolder : IDisposable
        {
            private bool disposed;
            private Coroutine c;
            
            private IEnumerator _c(SandboxFunc s, float wt)
            {
                while (!disposed)
                {
                    SandboxFuncTools.InvokeSandboxFunc(s);
                    yield return new WaitForSeconds(wt);
                }
            }

            public void Start(SandboxFunc s, float wt) => c = CoroutineRunner.Instance.StartCoroutine(_c(s, wt));

            public void Dispose()
            {
                disposed = true;
                try{CoroutineRunner.Instance.StopCoroutine(c);}catch(Exception){}
            }
        }
    }
}