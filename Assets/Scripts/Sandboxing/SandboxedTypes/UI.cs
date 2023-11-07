using System;
using Hypernex.Game;
using Hypernex.UI.Templates;
using Nexbox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class UI
    {
        public static void SetImageFromAsset(Item item, string asset)
        {
            Object a = SandboxTools.GetObjectFromWorldResource(asset);
            if (a == null)
                return;
            Image i = item.t.gameObject.GetComponent<Image>();
            if (i != null)
                i.sprite = (Sprite) a;
            RawImage ri = item.t.gameObject.GetComponent<RawImage>();
            if (ri != null)
                ri.texture = (Texture) a;
        }
        
        public static void RegisterButtonClick(Item item, SandboxFunc s)
        {
            Button b = item.t.gameObject.GetComponent<Button>();
            if(b != null)
                b.onClick.AddListener(() => SandboxFuncTools.InvokeSandboxFunc(s));
        }

        public static void RemoveAllButtonClicks(Item item)
        {
            Button b = item.t.gameObject.GetComponent<Button>();
            if(b != null)
                b.onClick.RemoveAllListeners();
        }

        public static string GetText(Item item)
        {
            TMP_Text tmpt = item.t.gameObject.GetComponent<TMP_Text>();
            if (tmpt != null)
                return tmpt.text;
            Text t = item.t.gameObject.GetComponent<Text>();
            if (t != null)
                return t.text;
            return String.Empty;
        }

        public static void SetText(Item item, string text)
        {
            TMP_Text tmpt = item.t.gameObject.GetComponent<TMP_Text>();
            if (tmpt != null)
                tmpt.text = text;
            Text t = item.t.gameObject.GetComponent<Text>();
            if (t != null)
                t.text = text;
        }

        public static bool GetToggle(Item item)
        {
            Toggle t = item.t.gameObject.GetComponent<Toggle>();
            if (t != null)
                return t.isOn;
            return false;
        }
        
        public static void RegisterToggleValueChanged(Item item, SandboxFunc s)
        {
            Toggle t = item.t.gameObject.GetComponent<Toggle>();
            if (t != null)
                t.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, t.isOn));
        }
        
        public static void RemoveAllToggleValueChanged(Item item)
        {
            Toggle t = item.t.gameObject.GetComponent<Toggle>();
            if (t != null)
                t.onValueChanged.RemoveAllListeners();
        }

        public static void SetToggle(Item item, bool value)
        {
            Toggle t = item.t.gameObject.GetComponent<Toggle>();
            if (t != null)
                t.isOn = value;
        }

        public static float GetSlider(Item item)
        {
            Slider slider = item.t.gameObject.GetComponent<Slider>();
            if (slider != null)
                return slider.value;
            return float.NaN;
        }
        
        public static void RegisterSliderValueChanged(Item item, SandboxFunc s)
        {
            Slider slider = item.t.gameObject.GetComponent<Slider>();
            if (slider != null)
                slider.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, slider.value));
        }
        
        public static void RemoveAllSliderValueChanged(Item item)
        {
            Slider slider = item.t.gameObject.GetComponent<Slider>();
            if (slider != null)
                slider.onValueChanged.RemoveAllListeners();
        }

        public static void SetSlider(Item item, float value)
        {
            Slider slider = item.t.gameObject.GetComponent<Slider>();
            if (slider != null)
                slider.value = value;
        }

        public static void SetSliderRange(Item item, float minimum, float maximum)
        {
            Slider slider = item.t.gameObject.GetComponent<Slider>();
            if (slider != null)
            {
                slider.minValue = minimum;
                slider.maxValue = maximum;
                slider.value = Mathf.Clamp(slider.value, minimum, maximum);
            }
        }
        
        public static float GetScrollbar(Item item)
        {
            Scrollbar scrollbar = item.t.gameObject.GetComponent<Scrollbar>();
            if (scrollbar != null)
                return scrollbar.value;
            return float.NaN;
        }

        public static void RegisterScrollbarValueChanged(Item item, SandboxFunc s)
        {
            Scrollbar scrollbar = item.t.gameObject.GetComponent<Scrollbar>();
            if (scrollbar != null)
                scrollbar.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, scrollbar.value));
        }
        
        public static void RemoveAllScrollbarValueChanged(Item item)
        {
            Scrollbar scrollbar = item.t.gameObject.GetComponent<Scrollbar>();
            if (scrollbar != null)
                scrollbar.onValueChanged.RemoveAllListeners();
        }

        public static void SetScrollbar(Item item, float value)
        {
            Scrollbar scrollbar = item.t.gameObject.GetComponent<Scrollbar>();
            if (scrollbar != null)
                scrollbar.value = value;
        }

        public static void RegisterInputFieldVR(Item item)
        {
            TMP_InputField tif = item.t.gameObject.GetComponent<TMP_InputField>();
            tif.onSelect.RemoveAllListeners();
            tif.onSelect.AddListener(_ =>
            {
                if (!LocalPlayer.IsVR)
                    return;
                KeyboardTemplate.GetKeyboardTemplateByLanguage("en").RequestInput(s => tif.text = s);
            });
        }
        
        public static string GetInputFieldText(Item item)
        {
            TMP_InputField tif = item.t.gameObject.GetComponent<TMP_InputField>();
            if (tif != null)
                return tif.text;
            InputField iff = item.t.gameObject.GetComponent<InputField>();
            if (iff != null)
                return iff.text;
            return String.Empty;
        }

        public static void RegisterInputFieldTextChanged(Item item, SandboxFunc s)
        {
            TMP_InputField tif = item.t.gameObject.GetComponent<TMP_InputField>();
            if (tif != null)
                tif.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, tif.text));
            InputField iff = item.t.gameObject.GetComponent<InputField>();
            if (iff != null)
                iff.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, iff.text));
        }
        
        public static void RemoveAllInputFieldTextChanged(Item item)
        {
            TMP_InputField tif = item.t.gameObject.GetComponent<TMP_InputField>();
            if (tif != null)
                tif.onValueChanged.RemoveAllListeners();
            InputField iff = item.t.gameObject.GetComponent<InputField>();
            if (iff != null)
                iff.onValueChanged.RemoveAllListeners();
        }

        public static void SetInputFieldText(Item item, string value)
        {
            TMP_InputField tif = item.t.gameObject.GetComponent<TMP_InputField>();
            if (tif != null)
                tif.text = value;
            InputField iff = item.t.gameObject.GetComponent<InputField>();
            if (iff != null)
                iff.text = value;
        }
        
        public static int GetDropdown(Item item)
        {
            TMP_Dropdown tdd = item.t.gameObject.GetComponent<TMP_Dropdown>();
            if (tdd != null)
                return tdd.value;
            Dropdown dd = item.t.gameObject.GetComponent<Dropdown>();
            if (dd != null)
                return dd.value;
            return -1;
        }

        public static void RegisterDropdownValueChanged(Item item, SandboxFunc s)
        {
            TMP_Dropdown tdd = item.t.gameObject.GetComponent<TMP_Dropdown>();
            if (tdd != null)
                tdd.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, tdd.value));
            Dropdown dd = item.t.gameObject.GetComponent<Dropdown>();
            if (dd != null)
                dd.onValueChanged.AddListener(_ => SandboxFuncTools.InvokeSandboxFunc(s, dd.value));
        }
        
        public static void RemoveAllDropdownValueChanged(Item item)
        {
            TMP_Dropdown tdd = item.t.gameObject.GetComponent<TMP_Dropdown>();
            if (tdd != null)
                tdd.onValueChanged.RemoveAllListeners();
            Dropdown dd = item.t.gameObject.GetComponent<Dropdown>();
            if (dd != null)
                dd.onValueChanged.RemoveAllListeners();
        }

        public static void SetDropdown(Item item, int value)
        {
            TMP_Dropdown tdd = item.t.gameObject.GetComponent<TMP_Dropdown>();
            if (tdd != null)
                tdd.value = value;
            Dropdown dd = item.t.gameObject.GetComponent<Dropdown>();
            if (dd != null)
                dd.value = value;
        }

        public static ColorBlock? GetColorBlock(Item item)
        {
            Button b = item.t.gameObject.GetComponent<Button>();
            if (b != null)
                return ColorBlock.FromUnityColorBlock(b.colors);
            Toggle t = item.t.gameObject.GetComponent<Toggle>();
            if (t != null)
                return ColorBlock.FromUnityColorBlock(t.colors);
            Slider slider = item.t.gameObject.GetComponent<Slider>();
            if (slider != null)
                return ColorBlock.FromUnityColorBlock(slider.colors);
            Scrollbar scrollbar = item.t.gameObject.GetComponent<Scrollbar>();
            if (scrollbar != null)
                return ColorBlock.FromUnityColorBlock(scrollbar.colors);
            TMP_InputField tif = item.t.gameObject.GetComponent<TMP_InputField>();
            if (tif != null)
                return ColorBlock.FromUnityColorBlock(tif.colors);
            InputField iff = item.t.gameObject.GetComponent<InputField>();
            if (iff != null)
                return ColorBlock.FromUnityColorBlock(iff.colors);
            TMP_Dropdown tdd = item.t.gameObject.GetComponent<TMP_Dropdown>();
            if(tdd != null)
                return ColorBlock.FromUnityColorBlock(tdd.colors);
            Dropdown dd = item.t.gameObject.GetComponent<Dropdown>();
            if(dd != null)
                return ColorBlock.FromUnityColorBlock(dd.colors);
            return null;
        }

        public static void SetColorBlock(Item item, ColorBlock colorBlock)
        {
            UnityEngine.UI.ColorBlock unityColorBlock = colorBlock.ToUnityColorBlock();
            Button b = item.t.gameObject.GetComponent<Button>();
            if (b != null)
                b.colors = unityColorBlock;
            Toggle t = item.t.gameObject.GetComponent<Toggle>();
            if (t != null)
                t.colors = unityColorBlock;
            Slider slider = item.t.gameObject.GetComponent<Slider>();
            if (slider != null)
                slider.colors = unityColorBlock;
            Scrollbar scrollbar = item.t.gameObject.GetComponent<Scrollbar>();
            if (scrollbar != null)
                scrollbar.colors = unityColorBlock;
            TMP_InputField tif = item.t.gameObject.GetComponent<TMP_InputField>();
            if (tif != null)
                tif.colors = unityColorBlock;
            InputField iff = item.t.gameObject.GetComponent<InputField>();
            if (iff != null)
                iff.colors = unityColorBlock;
            TMP_Dropdown tdd = item.t.gameObject.GetComponent<TMP_Dropdown>();
            if(tdd != null)
                tdd.colors = unityColorBlock;
            Dropdown dd = item.t.gameObject.GetComponent<Dropdown>();
            if(dd != null)
                dd.colors = unityColorBlock;
        }
    }
}