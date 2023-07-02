using TMPro;
using UnityEngine;

namespace Hypernex.Tools
{
    [RequireComponent(typeof(TMP_Text))]
    public class FPSCounter : MonoBehaviour
    {
        private TMP_Text text;

        private void OnEnable() => text = GetComponent<TMP_Text>();

        private void Update() => text.text = "FPS: " + Mathf.RoundToInt(1f / Time.unscaledDeltaTime);
    }
}