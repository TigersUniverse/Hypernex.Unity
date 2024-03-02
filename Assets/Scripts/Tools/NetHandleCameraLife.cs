using System;
using System.Collections;
using System.Threading;
using Hypernex.Game;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hypernex.Tools
{
    public class NetHandleCameraLife : IDisposable
    {
        public Transform transform { get; private set; }

        public SmoothTransform SmoothTransform => smoothTransform ??= new SmoothTransform(transform, false);

        private SmoothTransform smoothTransform;
        private readonly CancellationTokenSource cts = new();
        private bool hasPinged;
        private readonly Action onDispose;
        private Coroutine c1;
        private Coroutine c2;
        private TMP_Text t;
        
        public NetHandleCameraLife(User u, Transform transform, Action onDispose)
        {
            this.transform = transform;
            this.onDispose = onDispose;
            c1 = CoroutineRunner.Instance.StartCoroutine(Loop1());
            t = transform.Find("Username").GetComponent<TMP_Text>();
            t.text = $"@{u.Username}";
            c2 = CoroutineRunner.Instance.StartCoroutine(Loop2());
        }

        public void Ping() => hasPinged = true;

        private IEnumerator Loop1()
        {
            while (!cts.IsCancellationRequested)
            {
                yield return new WaitForSeconds(2);
                if (!hasPinged)
                    Dispose();
                else
                    hasPinged = false;
            }
        }

        private IEnumerator Loop2()
        {
            while (!cts.IsCancellationRequested)
            {
                Camera MainCamera = LocalPlayer.Instance.Camera;
                t.transform.rotation =
                    Quaternion.LookRotation((transform.position - MainCamera.transform.position).normalized);
                yield return null;
            }
        }

        public void Dispose()
        {
            onDispose.Invoke();
            cts.Cancel();
            CoroutineRunner.Instance.StopCoroutine(c1);
            CoroutineRunner.Instance.StopCoroutine(c2);
            smoothTransform?.Dispose();
        }
    }
}