using System;
using UnityEngine;
#if UNITY_EDITOR
using TriInspector;
#endif

namespace Hypernex.CCK.Unity.Assets
{
    [Serializable]
#if UNITY_EDITOR
    [HideMonoScript]
#endif
    [CreateAssetMenu(fileName = "Avatar Menu", menuName = "Hypernex/Avatars/Menu")]
    public class AvatarMenu : ScriptableObject
    {
#if UNITY_EDITOR
        [ValidateInput(nameof(ValidParameters))]
#endif
        public AvatarParameters Parameters;

#if UNITY_EDITOR
        [ListDrawerSettings(AlwaysExpanded = true)]
#endif
        public AvatarControl[] Controls = Array.Empty<AvatarControl>();

#if UNITY_EDITOR
        private TriValidationResult ValidParameters()
        {
            if (Parameters == null) return TriValidationResult.Error("Please set your Avatar's Parameters!");
            if (Parameters.Parameters.Length <= 0) return TriValidationResult.Warning("No parameters are present!");
            return TriValidationResult.Valid;
        }
#endif
    }

    [Serializable]
    public class AvatarControl
    {
        public string ControlName;
        public Sprite ControlSprite;
        public ControlType ControlType;
        public string[] DropdownOptions = Array.Empty<string>();
        public int TargetParameterIndex;
        public int TargetParameterIndex2;
        public AvatarMenu SubMenu;
    }

    public enum ControlType
    {
        Toggle,
        Slider,
        Dropdown,
        TwoDimensionalAxis,
        SubMenu
    }
}