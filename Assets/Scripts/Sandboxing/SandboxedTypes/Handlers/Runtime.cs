using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Tools;
using Nexbox;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Sandboxing.SandboxedTypes.Handlers
{
    public class Runtime
    {
        private Dictionary<object, SandboxFunc> onFixedUpdates = new ();
        
        private Dictionary<object, SandboxFunc> onUpdates = new ();

        private Dictionary<object, SandboxFunc> onLateUpdates = new();
        
        private Dictionary<object, SandboxFunc> onDisposals = new();

        private Dictionary<object, CoroutineHolder> repeats = new();
        
        public void OnFixedUpdate(object s) => onFixedUpdates.Add(s, SandboxFuncTools.TryConvert(s));
        public void RemoveOnFixedUpdate(object s) => onFixedUpdates.Remove(s);
        public void OnUpdate(object s) => onUpdates.Add(s, SandboxFuncTools.TryConvert(s));
        public void RemoveOnUpdate(object s) => onUpdates.Remove(s);
        public void OnLateUpdate(object s) => onLateUpdates.Add(s, SandboxFuncTools.TryConvert(s));
        public void RemoveOnLateUpdate(object s) => onLateUpdates.Remove(s);
        public void OnDispose(object s) => onDisposals.Add(s, SandboxFuncTools.TryConvert(s));
        public void RemoveOnDispose(object s) => onDisposals.Remove(s);

        public void RepeatSeconds(object s, float waitTime)
        {
            CoroutineHolder c = new CoroutineHolder();
            SandboxFunc sandboxFunc = SandboxFuncTools.TryConvert(s);
            repeats.Add(s, c);
            c.Start(sandboxFunc, waitTime);
        }

        public void RemoveRepeatSeconds(object s) => repeats.Remove(s);

        public void RunAfterSeconds(object s, float time) =>
            CoroutineRunner.Instance.StartCoroutine(_w(SandboxFuncTools.TryConvert(s), time));

        internal void FixedUpdate()
        {
            foreach (var x in onFixedUpdates.Values)
            {
                try
                {
                    SandboxFuncTools.InvokeSandboxFunc(x);
                }
                catch (Exception e)
                {
                    Logger.CurrentLogger.Error(e);
                }
            }
        }

        internal void Update()
        {
            foreach (var x in onUpdates.Values)
            {
                try
                {
                    SandboxFuncTools.InvokeSandboxFunc(x);
                }
                catch (Exception e)
                {
                    Logger.CurrentLogger.Error(e);
                }
            }
        }
        
        internal void LateUpdate()
        {
            foreach (var x in onLateUpdates.Values)
            {
                try
                {
                    SandboxFuncTools.InvokeSandboxFunc(x);
                }
                catch (Exception e)
                {
                    Logger.CurrentLogger.Error(e);
                }
            }
        }

        private IEnumerator _w(SandboxFunc s, float t)
        {
            yield return new WaitForSeconds(t);
            SandboxFuncTools.InvokeSandboxFunc(s);
        }
        
        internal void Dispose()
        {
            foreach (SandboxFunc onDisposalsFunc in onDisposals.Values)
                try
                {
                    SandboxFuncTools.InvokeSandboxFunc(onDisposalsFunc);
                }
                catch (Exception e)
                {
                    Logger.CurrentLogger.Error(e);
                }
            onFixedUpdates.Clear();
            onUpdates.Clear();
            onLateUpdates.Clear();
            onDisposals.Clear();
            foreach (CoroutineHolder coroutineHolder in repeats.Values)
                coroutineHolder.Dispose();
            repeats.Clear();
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