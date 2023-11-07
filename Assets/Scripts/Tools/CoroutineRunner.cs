using System.Collections;
using UnityEngine;

namespace Hypernex.Tools
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class CoroutineRunner : MonoBehaviour
    {
        public static CoroutineRunner Instance { get; private set; }

        public Coroutine Run(IEnumerator enumerator) => StartCoroutine(enumerator);
        public void Stop(Coroutine c) => StopCoroutine(c);

        private void Start() => Instance = this;
    }
}