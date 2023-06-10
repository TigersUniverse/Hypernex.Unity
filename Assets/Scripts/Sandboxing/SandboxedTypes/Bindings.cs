using System;
using System.Collections.Generic;
using Hypernex.Game;
using Hypernex.Game.Bindings;
using Nexbox;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    public static class Bindings
    {
        public static string[] GetAllPresentBindings()
        {
            List<string> binding = new List<string>();
            foreach (IBinding instanceBinding in LocalPlayer.Instance.Bindings)
                binding.Add(instanceBinding.Id);
            return binding.ToArray();
        }

        private static IBinding GetBinding(string bindingId)
        {
            foreach (IBinding instanceBinding in LocalPlayer.Instance.Bindings)
                if (instanceBinding.Id == bindingId)
                    return instanceBinding;
            return null;
        }

        public static float GetUp(string binding) => GetBinding(binding)?.Up ?? 0.00f;
        public static float GetDown(string binding) => GetBinding(binding)?.Down ?? 0.00f;
        public static float GetLeft(string binding) => GetBinding(binding)?.Left ?? 0.00f;
        public static float GetRight(string binding) => GetBinding(binding)?.Right ?? 0.00f;
        public static bool GetButton(string binding) => GetBinding(binding)?.Button ?? false;
        public static bool GetButton2(string binding) => GetBinding(binding)?.Button2 ?? false;
        public static float GetTrigger(string binding) => GetBinding(binding)?.Trigger ?? 0.00f;
        public static bool GetGrab(string binding) => GetBinding(binding)?.Grab ?? false;

        public static void RegisterButtonClick(string binding, SandboxFunc f)
        {
            IBinding b = GetBinding(binding);
            if (b != null)
                b.ButtonClick += () => SandboxFuncTools.InvokeSandboxFunc(f, b.Button);
        }
        public static void RegisterButton2Click(string binding, SandboxFunc f)
        {
            IBinding b = GetBinding(binding);
            if (b != null)
                b.Button2Click += () => SandboxFuncTools.InvokeSandboxFunc(f, b.Button2);
        }
        public static void RegisterTriggerClick(string binding, SandboxFunc f)
        {
            IBinding b = GetBinding(binding);
            if (b != null)
                b.TriggerClick += () => SandboxFuncTools.InvokeSandboxFunc(f, b.Trigger);
        }

        public static void RegisterKeyboardClick(string keyCode, SandboxFunc f)
        {
            IBinding b = GetBinding("Keyboard");
            if (b != null)
                ((Keyboard) b).RegisterCustomKeyDownEvent((KeyCode) Enum.Parse(typeof(KeyCode), keyCode),
                    () => SandboxFuncTools.InvokeSandboxFunc(f));
        }

        public static void RegisterMouseClick(int mouseId, SandboxFunc f)
        {
            IBinding b = GetBinding("Mouse");
            if (b != null)
                ((Mouse) b).RegisterCustomMouseButtonDownEvent(mouseId, () => SandboxFuncTools.InvokeSandboxFunc(f));
        }
    }
}