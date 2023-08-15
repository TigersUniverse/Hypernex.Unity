using TMPro;
using UnityEngine;

namespace Hypernex.Tools
{
    [RequireComponent(typeof(TMP_Text))]
    public class FPSCounter : MonoBehaviour
    {
        public static float FPS { get; private set; }
        
        private TMP_Text text;

        private void OnEnable() => text = GetComponent<TMP_Text>();

        private void Update()
        {
            FPS = 1f / Time.unscaledDeltaTime;
            text.text = "FPS: " + Mathf.RoundToInt(FPS);
        }
    }
}