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
        public GameObject RespawnButton;

        public void RespawnPlayer()
        {
            if (LocalPlayer.Instance == null)
                return;
            LocalPlayer.Instance.Respawn();
        }

        private void Update()
        {
            TargetImage.sprite = LocalPlayer.IsVR ? DesktopSprite : VRSprite;
            bool enableRespawn = true;
            if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null)
                enableRespawn = GameInstance.FocusedInstance.World.AllowRespawn;
            RespawnButton.SetActive(enableRespawn);
        }
    }
}