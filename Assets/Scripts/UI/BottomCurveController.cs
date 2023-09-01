using Hypernex.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI
{
    public class BottomCurveController : MonoBehaviour
    {
        public Image TargetImage;
        public Sprite DesktopSprite;
        public Sprite VRSprite;

        private void Update() => TargetImage.sprite = LocalPlayer.IsVR ? DesktopSprite : VRSprite;
    }
}