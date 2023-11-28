using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Tools;
using Nexbox;
using NUnit.Framework;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public class Runtime : IDisposable
    {
        internal Dictionary<object, SandboxFunc> OnFixedUpdates => new (onFixedUpdates);
        private Dictionary<object, SandboxFunc> onFixedUpdates = new ();
        
        internal Dictionary<object, SandboxFunc> OnUpdates => new (onUpdates);
        private Dictionary<object, SandboxFunc> onUpdates = new ();

        internal Dictionary<object, SandboxFunc> OnLateUpdates => new(onLateUpdates);
        private Dictionary<object, SandboxFunc> onLateUpdates = new();

        private Dictionary<object, CoroutineHolder> repeats = new();
        
        public void OnFixedUpdate(object s) => onFixedUpdates.Add(s, SandboxFuncTools.TryConvert(s));
        public void RemoveOnFixedUpdate(object s) => onFixedUpdates.Remove(s);
        public void OnUpdate(object s) => onUpdates.Add(s, SandboxFuncTools.TryConvert(s));
        public void RemoveOnUpdate(object s) => onUpdates.Remove(s);
        public void OnLateUpdate(object s) => onLateUpdates.Add(s, SandboxFuncTools.TryConvert(s));
        public void RemoveOnLateUpdate(object s) => onLateUpdates.Remove(s);

        public void RepeatSeconds(object s, float waitTime)
        {
            CoroutineHolder c = new CoroutineHolder();
            SandboxFunc sandboxFunc = SandboxFuncTools.TryConvert(s);
            repeats.Add(sandboxFunc, c);
            c.Start(sandboxFunc, waitTime);
        }

        public void RunAfterSeconds(object s, float time) =>
            CoroutineRunner.Instance.StartCoroutine(_w(SandboxFuncTools.TryConvert(s), time));

        public void FixedUpdate() => OnFixedUpdates.Values.ToList().ForEach(x =>
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

        public void Update() => OnUpdates.Values.ToList().ForEach(x =>
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
        
        public void LateUpdate() => OnLateUpdates.Values.ToList().ForEach(x =>
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
            List<CoroutineHolder> coroutineHolders = new List<CoroutineHolder>(repeats.Values);
            for (int i = 0; i < coroutineHolders.Count; i++)
            {
                CoroutineHolder coroutineHolder = coroutineHolders[i];
                coroutineHolder.Dispose();
                repeats.Remove(repeats.ElementAt(i).Key);
            }
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