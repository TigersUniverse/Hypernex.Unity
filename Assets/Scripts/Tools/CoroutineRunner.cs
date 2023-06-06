using System.Collections;
using UnityEngine;

namespace Hypernex.Tools
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class CoroutineRunner : MonoBehaviour
    {
        public static CoroutineRunner Instance { get; private set; }

        public void Run(IEnumerator enumerator) => StartCoroutine(enumerator);

        private void Start() => Instance = this;
    }
}