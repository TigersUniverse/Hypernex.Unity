using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.Tools
{
    public class FPSCounter : MonoBehaviour
    {
        public static float FPS { get; private set; }
        public static float TargetFPS => Application.targetFrameRate;

        public bool Smooth;
        public Color GoodFPS = Color.green;
        public Color BadFPS = Color.red;
        public TMP_Text[] TargetTexts;
        public Image[] TargetColors;
        private Coroutine c;

        private void OnEnable() => c = StartCoroutine(UpdateFPS());

        private IEnumerator UpdateFPS()
        {
            while (true)
            {
                if(Smooth)
                    FPS = 1f / Time.unscaledDeltaTime;
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void Update()
        {
            if(!Smooth)
                FPS = 1f / Time.unscaledDeltaTime;
            foreach (TMP_Text targetText in TargetTexts)
                targetText.text = "FPS: " + Mathf.RoundToInt(FPS);
            float percentage;
            if (TargetFPS <= 0)
                percentage = FPS / 60;
            else
                percentage = FPS / TargetFPS;
            if (percentage > 1)
                percentage = 1;
            if (percentage < 0)
                percentage = 0;
            Color targetColor = Color.Lerp(BadFPS, GoodFPS, percentage);
            foreach (Image targetImage in TargetColors)
                targetImage.color = targetColor;
        }

        private void OnDisable()
        {
            if(c != null)
                StopCoroutine(c);
        }
    }
}