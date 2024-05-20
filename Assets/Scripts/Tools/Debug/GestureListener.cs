using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Game.Avatar.FingerInterfacing;
using UnityEngine;

namespace Hypernex.Tools.Debug
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class GestureListener : MonoBehaviour
    {
        private string GetCurlText(IFingerCurler fingerCurler) => "Thumb: " + fingerCurler.ThumbCurl + "\n" +
                                                             "Index: " + fingerCurler.IndexCurl + "\n" +
                                                             "Middle: " + fingerCurler.MiddleCurl + "\n" +
                                                             "Ring: " + fingerCurler.RingCurl + "\n" +
                                                             "Pinky: " + fingerCurler.PinkyCurl;
        
        private void OnGUI()
        {
            LocalPlayer localPlayer = LocalPlayer.Instance;
            if(localPlayer == null) return;
            if(localPlayer.avatar == null) return;
            if (localPlayer.avatar.fingerCalibration == null) return;
            IFingerCurler left = localPlayer.GetLeftHandCurler();
            IFingerCurler right = localPlayer.GetRightHandCurler();
            GUILayout.BeginArea(new Rect(50, 10, 500, Screen.height));
            GUILayout.Label("LeftHand:\n" + GetCurlText(left));
            GUILayout.Label("GestureLeft: " + FingerCalibration.GetGestureNumberFromHandGetter(left));
            GUILayout.Label("RightHand:\n" + GetCurlText(right));
            GUILayout.Label("GestureRight: " + FingerCalibration.GetGestureNumberFromHandGetter(right));
            GUILayout.EndArea();
        }
    }
}