using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity.Assets;
using Hypernex.Tools;
using Hypernex.UI.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction.AvatarControllers
{
    public class DropdownContainer : MonoBehaviour, IRender<(AvatarControl, AvatarParameter)>
    {
        public TMP_Text ControlText;
        public Image Icon;
        public RectTransform Content;

        private List<DropdownControl> Dropdowns = new();
        
        public void Render((AvatarControl, AvatarParameter) t)
        {
            ControlText.text = t.Item1.ControlName;
            if(t.Item1.ControlSprite != null) Icon.sprite = t.Item1.ControlSprite;
            Dropdowns.Clear();
            for (int _ = 0; _ < t.Item1.DropdownOptions.Length; _++)
                CreateDropdown();
            ToggleButton[] family = Dropdowns.Select(x => x.ToggleButton).ToArray();
            foreach (DropdownControl dropdownControl in Dropdowns)
                dropdownControl.ToggleButton.Family = family;
            foreach (DropdownControl dropdownControl in Dropdowns)
                dropdownControl.Render(t);
        }

        private void CreateDropdown()
        {
            IRender<(AvatarControl, AvatarParameter)> dropdownRenderer =
                Defaults.GetRenderer<(AvatarControl, AvatarParameter)>("AvatarDropdownItem");
            Content.AddChild(dropdownRenderer.transform);
            DropdownControl d = (DropdownControl) dropdownRenderer;
            Dropdowns.Add(d);
        }
    }
}