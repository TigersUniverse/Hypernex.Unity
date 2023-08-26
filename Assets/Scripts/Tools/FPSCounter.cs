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
        
        private TMP_Text text;
        private Slider slider;
        private Image img;
        private Coroutine c;

        private void OnEnable()
        {
            text = GetComponent<TMP_Text>();
            slider = GetComponent<Slider>();
            if (slider != null)
                img = slider.fillRect.GetComponent<Image>();
            c = StartCoroutine(UpdateFPS());
        }

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
            if(text != null)
                text.text = "FPS: " + Mathf.RoundToInt(FPS);
            if (slider != null)
            {
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
                img.color = targetColor;
                slider.value = Mathf.Lerp(slider.value, percentage, 0.05f);
            }
        }

        private void OnDisable()
        {
            if(c != null)
                StopCoroutine(c);
        }
    }
}